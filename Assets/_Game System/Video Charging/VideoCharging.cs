using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class VideoCharging : MonoBehaviour
{
    //public events 
    public static event Action OnNextLevelToggled;



    [Header("Charge Settings")]
    [SerializeField] private float chargeRadius = 2f;
    [SerializeField] private float chargeDuration = 3f;
    [SerializeField] private bool usePlayerTag = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Static Visualization")]
    [SerializeField] private Transform chargeRadiusVisualization;
    [SerializeField] private float visualizationGrowDuration = 1f;

    [Header("Pulse Animation")]
    [SerializeField] private SpriteRenderer chargeLoopVisual;
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float maxOpacity = 4f;

    [Header("Video & UI States")]
    [SerializeField] private GameObject videoGameObject;
    [Tooltip("The UI group/tab active during the charging process.")]
    [SerializeField] private GameObject chargingTab;
    [Tooltip("Object that appears only when charging is 100% complete.")]
    [SerializeField] private GameObject videoDoneObject;
    [Tooltip("Displayed when player is NOT charging (paused).")]
    [SerializeField] private GameObject videoStopObject;
    [SerializeField] private Image chargeFillImage;

    [Header("Spawner Reference")]
    [SerializeField] private DirectorSpawner2D spawner;

    private float _originalAmbushChance;

    private float _currentCharge;
    private bool _chargeComplete;
    private bool _isVideoStarted;
    private float _visualizationTimer;
    
    private Vector3 _staticDefaultScale = Vector3.one;
    private Vector3 _pulseTargetScale = Vector3.one;
    private Sequence _pulseSequence;
    private bool _isFadingOut;

    private void Awake()
    {
        if (chargeRadiusVisualization != null)
        {
            _staticDefaultScale = chargeRadiusVisualization.localScale;
            chargeRadiusVisualization.localScale = Vector3.zero;
        }

        if (chargeLoopVisual != null)
        {
            _pulseTargetScale = chargeLoopVisual.transform.localScale;
            chargeLoopVisual.transform.localScale = Vector3.zero;
            Color c = chargeLoopVisual.color;
            c.a = 0f;
            chargeLoopVisual.color = c;
            chargeLoopVisual.gameObject.SetActive(false);
        }

        // Initialize object states
        if (videoGameObject != null) videoGameObject.SetActive(false);
        if (videoDoneObject != null) videoDoneObject.SetActive(false);
        if (chargingTab != null) chargingTab.SetActive(false);
        if (videoStopObject != null) videoStopObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isVideoStarted || _chargeComplete) return;

        bool isInside = DetectPlayerInsideRadius();

        if (isInside)
        {
            _currentCharge += Time.deltaTime;
            HandlePulseAnimation(true);

            if (videoStopObject != null && videoStopObject.activeSelf)
                videoStopObject.SetActive(false);

            if (_currentCharge >= chargeDuration)
            {
                _currentCharge = chargeDuration;
                OnChargeComplete();
            }
        }
        else
        {
            HandlePulseAnimation(false);

            if (videoStopObject != null && !videoStopObject.activeSelf)
                videoStopObject.SetActive(true);
        }

        UpdateFill();
        UpdateStaticVisualization();
    }

    private void HandlePulseAnimation(bool active)
    {
        if (chargeLoopVisual == null) return;

        if (active)
        {
            if (_isFadingOut)
            {
                _pulseSequence?.Kill();
                _isFadingOut = false;
            }

            if (_pulseSequence == null || !_pulseSequence.IsActive())
            {
                chargeLoopVisual.gameObject.SetActive(true);
                _pulseSequence = DOTween.Sequence();
                
                _pulseSequence.AppendCallback(() => {
                    chargeLoopVisual.transform.localScale = Vector3.zero;
                    Color c = chargeLoopVisual.color;
                    c.a = maxOpacity; 
                    chargeLoopVisual.color = c;
                });

                _pulseSequence.Append(chargeLoopVisual.transform.DOScale(_pulseTargetScale, pulseDuration).SetEase(Ease.OutQuad));
                _pulseSequence.Join(chargeLoopVisual.DOFade(0f, pulseDuration).SetEase(Ease.InQuad));
                _pulseSequence.SetLoops(-1, LoopType.Restart);
            }
        }
        else
        {
            if (_pulseSequence != null && _pulseSequence.IsActive() && !_isFadingOut)
            {
                _isFadingOut = true;
                _pulseSequence.Kill();
                _pulseSequence = DOTween.Sequence();
                _pulseSequence.Join(chargeLoopVisual.DOFade(0f, 0.4f));
                _pulseSequence.OnComplete(() => {
                    chargeLoopVisual.gameObject.SetActive(false);
                    _isFadingOut = false;
                });
            }
        }
    }

    private void OnChargeComplete()
    {
        _chargeComplete = true;
        Debug.Log("Charge Complete!");

        // 1. Wipe all active enemies
        if (spawner != null)
        {
            spawner.WipeAllEnemies();
            spawner.ambushChance = _originalAmbushChance;
        }

        // 2. Clean up animations
        HandlePulseAnimation(false);

        // 3. Shrink the static radius visual to 0
        if (chargeRadiusVisualization != null)
        {
            chargeRadiusVisualization.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        }

        // 4. Toggle Objects
        if (chargingTab != null) chargingTab.SetActive(false);
        if (videoStopObject != null) videoStopObject.SetActive(false);
        if (videoDoneObject != null) videoDoneObject.SetActive(true);
    }


    public void NextLevel()
    {
        if(_chargeComplete == false) return;
        OnNextLevelToggled?.Invoke();
        
    }

    private void UpdateStaticVisualization()
    {
        // Only run growth logic if video just started and not yet complete
        if (chargeRadiusVisualization == null || _chargeComplete) return;

        if (_visualizationTimer < visualizationGrowDuration)
        {
            _visualizationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_visualizationTimer / visualizationGrowDuration);
            chargeRadiusVisualization.localScale = _staticDefaultScale * t;
        }
    }

    public void StartVideo()
    {
        if (_isVideoStarted) return;
        _isVideoStarted = true;
        _visualizationTimer = 0f;
        _currentCharge = 0f;
        _chargeComplete = false;

        // Disable 30% of current active enemies and boost ambush to 70%
        if (spawner != null)
        {
            spawner.DisablePercentageOfEnemies(0.3f);
            _originalAmbushChance = spawner.ambushChance;
            spawner.ambushChance = 0.7f;
        }

        if (videoGameObject != null) videoGameObject.SetActive(true);
        if (chargingTab != null) chargingTab.SetActive(true);
        if (videoStopObject != null) videoStopObject.SetActive(true);
        if (videoDoneObject != null) videoDoneObject.SetActive(false);
    }

    private bool DetectPlayerInsideRadius()
    {
        if (chargeRadius <= 0f) return false;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, chargeRadius, playerLayerMask);
        
        if (usePlayerTag && hit != null)
        {
            return hit.CompareTag(playerTag);
        }
        return hit != null;
    }

    private void UpdateFill()
    {
        if (chargeFillImage != null)
            chargeFillImage.fillAmount = Mathf.Clamp01(_currentCharge / chargeDuration);
    }

    private void OnDestroy() => _pulseSequence?.Kill();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chargeRadius);
    }
}