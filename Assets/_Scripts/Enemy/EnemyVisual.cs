using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace _Scripts.Enemy
{
    public class EnemyVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private SpriteRenderer[] flashVisuals;
        [SerializeField] private ParticleSystem deathParticles;

        [Header("Flash Events")]
        public UnityEvent OnFlashWindupStart = new();
        public UnityEvent OnFlashComplete = new();

        [Header("Juice Settings")]
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float shakeStrength = 15f;
        [SerializeField] private Color damageColor = Color.white;
        private float flashDuration = 0.05f;

        [Header("Scale Tween")]
        [SerializeField] private Transform scaleTarget;
        [SerializeField] private float showScaleDuration = 0.2f;
        [SerializeField] private float hideScaleDuration = 0.15f;
        [SerializeField] private Ease showScaleEase = Ease.OutBack;
        [SerializeField] private Ease hideScaleEase = Ease.InBack;

        private Color[] _baseColors;
        private Transform _scaleTarget;
        private bool _isInitialized = false;

        private Transform ScaleTarget => _scaleTarget != null ? _scaleTarget : transform;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            
            _scaleTarget = scaleTarget != null ? scaleTarget : transform;
            if (shakeTarget == null) shakeTarget = _scaleTarget;

            if (flashVisuals != null && flashVisuals.Length > 0)
            {
                _baseColors = new Color[flashVisuals.Length];
                for (int i = 0; i < flashVisuals.Length; i++)
                {
                    if (flashVisuals[i] != null)
                    {
                        _baseColors[i] = flashVisuals[i].color;
                    }
                }
            }
            _isInitialized = true;
        }

        private void OnEnable()
        {
            Initialize(); // Ensure base color is captured before applying it
            
            if (ScaleTarget != null)
            {
                ScaleTarget.DOKill();
                ScaleTarget.localScale = Vector3.zero;
                ScaleTarget
                    .DOScale(new Vector3(1.1f, 1.1f, 1.1f), showScaleDuration)
                    .SetEase(showScaleEase);
            }

            if (shakeTarget != null)
            {
                // Reset shake target rotation 
                shakeTarget.DOKill();
                shakeTarget.localRotation = Quaternion.identity;
            }

            if (flashVisuals != null)
            {
                for (int i = 0; i < flashVisuals.Length; i++)
                {
                    var visual = flashVisuals[i];
                    if (visual != null && _baseColors != null)
                    {
                        // Reset visual state for pooling safety
                        visual.DOKill();
                        visual.color = _baseColors[i];
                        visual.enabled = true;
                    }
                }
            }
        }

        public void PlayHitEffects()
        {
            OnFlashWindupStart?.Invoke();
            
            if (shakeTarget != null)
            {
                // Shake Rotation
                shakeTarget.DOKill(true); 
                shakeTarget.localRotation = Quaternion.identity; 
                shakeTarget.DOShakeRotation(shakeDuration, new Vector3(0, 0, shakeStrength));
            }

            if (flashVisuals != null)
            {
                bool invokeComplete = true; // Make sure the event is only invoked once per flash
                for (int i = 0; i < flashVisuals.Length; i++)
                {
                    var index = i;
                    var visual = flashVisuals[index];
                    if (visual != null && _baseColors != null)
                    {
                        visual.DOKill();
                        var tween = visual.DOColor(damageColor, flashDuration)
                            .SetLoops(2, LoopType.Yoyo);

                        if (invokeComplete)
                        {
                            tween.OnComplete(() => 
                            {
                                visual.color = _baseColors[index];
                                OnFlashComplete?.Invoke();
                            });
                            invokeComplete = false;
                        }
                        else
                        {
                            tween.OnComplete(() => visual.color = _baseColors[index]);
                        }
                    }
                }
                
                // Fallback invoke if empty list or missing visuals
                if (invokeComplete)
                {
                    DOVirtual.DelayedCall(flashDuration * 2, () => OnFlashComplete?.Invoke());
                }
            }
            else
            {
                 DOVirtual.DelayedCall(flashDuration * 2, () => OnFlashComplete?.Invoke());
            }
        }

        public void PlayDeathEffects()
        {
            if (shakeTarget != null) shakeTarget.DOKill();

            if (ScaleTarget != null)
            {
                ScaleTarget.DOKill();
                
                if (flashVisuals != null)
                {
                    foreach (var visual in flashVisuals)
                    {
                        if (visual != null)
                        {
                            visual.DOKill();
                            visual.enabled = true;
                        }
                    }
                }
                
                ScaleTarget
                    .DOScale(Vector3.zero, hideScaleDuration)
                    .SetEase(hideScaleEase)
                    .OnComplete(() => 
                    {
                        if (flashVisuals != null)
                        {
                            foreach (var visual in flashVisuals)
                            {
                                if (visual != null) visual.enabled = false;
                            }
                        }
                    });
            }

            if (deathParticles != null)
            {
                deathParticles.Play();
            }
        }

        public void HideVisual()
        {
            if (flashVisuals != null)
            {
                foreach (var visual in flashVisuals)
                {
                    if (visual != null)
                    {
                        visual.DOKill();
                        visual.enabled = false;
                    }
                }
            }
        }

        public void ShowVisual()
        {
            if (flashVisuals != null && _baseColors != null)
            {
                for (int i = 0; i < flashVisuals.Length; i++)
                {
                    var visual = flashVisuals[i];
                    if (visual != null)
                    {
                        visual.enabled = true;
                        visual.color = _baseColors[i];
                    }
                }
            }
        }
    }
}