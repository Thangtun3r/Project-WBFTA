using UnityEngine;
using UnityEngine.UI;

public class VideoCharging : MonoBehaviour
{
    [Header("Charge Settings")]
    [Tooltip("Radius used to detect the player staying inside the charge area.")]
    [SerializeField] private float chargeRadius = 2f;

    [Tooltip("How many seconds the player must remain inside to reach full charge.")]
    [SerializeField] private float chargeDuration = 3f;

    [Tooltip("If enabled, player detection uses the tag. Otherwise use the layer mask.")]
    [SerializeField] private bool usePlayerTag = true;

    [Tooltip("Player tag used when detecting by tag.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Layer mask used when detecting by layer.")]
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Visualization")]
    [Tooltip("Optional transform used to visualize the charge radius. It will start at scale 0 and grow as the video starts.")]
    [SerializeField] private Transform chargeRadiusVisualization;

    [Tooltip("Time in seconds for the radius visualization to scale from 0 to its default size after StartVideo is called.")]
    [SerializeField] private float visualizationGrowDuration = 1f;

    [Header("Video")]
    [Tooltip("Optional GameObject that will be activated when StartVideo is called.")]
    [SerializeField] private GameObject videoGameObject;

    [Header("UI")]
    [Tooltip("UGUI Image that will fill from 0 to 1 as the charge progresses.")]
    [SerializeField] private Image chargeFillImage;

    private float _currentCharge;
    private bool _chargeComplete;
    private bool _isVideoStarted;
    private float _visualizationTimer;
    private Vector3 _defaultVisualizationScale = Vector3.one;

    private void Awake()
    {
        if (chargeRadiusVisualization != null)
        {
            _defaultVisualizationScale = chargeRadiusVisualization.localScale;
            chargeRadiusVisualization.localScale = Vector3.zero;
        }

        if (videoGameObject != null)
        {
            videoGameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!_isVideoStarted)
        {
            return;
        }

        bool isInside = DetectPlayerInsideRadius();

        if (isInside)
        {
            _currentCharge += Time.deltaTime;
            if (_currentCharge >= chargeDuration)
            {
                _currentCharge = chargeDuration;
                if (!_chargeComplete)
                {
                    _chargeComplete = true;
                    OnChargeComplete();
                }
            }
        }

        UpdateFill();
        UpdateVisualizationScale();
    }

    private bool DetectPlayerInsideRadius()
    {
        if (chargeRadius <= 0f)
        {
            return false;
        }

        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, chargeRadius, playerLayerMask);

        if (usePlayerTag)
        {
            foreach (Collider2D hit in hits)
            {
                if (hit != null && hit.CompareTag(playerTag))
                {
                    return true;
                }
            }
            return false;
        }

        return hits.Length > 0;
    }

    private void UpdateFill()
    {
        if (chargeFillImage == null) return;
        float fillAmount = chargeDuration > 0f ? Mathf.Clamp01(_currentCharge / chargeDuration) : 0f;
        chargeFillImage.fillAmount = fillAmount;
    }

    private void OnChargeComplete()
    {
        Debug.Log($"{name}: Charge complete.");
    }

    private void UpdateVisualizationScale()
    {
        if (chargeRadiusVisualization == null)
        {
            return;
        }

        if (_visualizationTimer < visualizationGrowDuration && visualizationGrowDuration > 0f)
        {
            _visualizationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_visualizationTimer / visualizationGrowDuration);
            chargeRadiusVisualization.localScale = _defaultVisualizationScale * t;
            return;
        }

        chargeRadiusVisualization.localScale = _defaultVisualizationScale;
    }

    public void StartVideo()
    {
        if (_isVideoStarted)
        {
            return;
        }

        _isVideoStarted = true;
        _visualizationTimer = 0f;
        ResetCharge();

        if (chargeRadiusVisualization != null)
        {
            chargeRadiusVisualization.localScale = Vector3.zero;
        }

        if (videoGameObject != null)
        {
            videoGameObject.SetActive(true);
        }
    }

    public void ResetCharge()
    {
        _currentCharge = 0f;
        _chargeComplete = false;
        UpdateFill();

        if (chargeRadiusVisualization != null)
        {
            chargeRadiusVisualization.localScale = Vector3.zero;
        }
    }

    public float GetChargeProgress()
    {
        return chargeDuration > 0f ? Mathf.Clamp01(_currentCharge / chargeDuration) : 0f;
    }

    public bool IsFullyCharged => _chargeComplete;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chargeRadius);
    }
}
