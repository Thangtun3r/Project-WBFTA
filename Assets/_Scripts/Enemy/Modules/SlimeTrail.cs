using UnityEngine;
using DG.Tweening;

namespace _Scripts.Enemy.Modules
{
    public class SlimeTrail : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private float lifetime = 2f;        // How long it stays fully visible
        [SerializeField] private float fadeDuration = 1.5f;  // How long it takes to fade out
        [SerializeField] private SpriteRenderer spriteRenderer;

        private float _damage;
        private Color _baseColor;
        private Vector3 _baseScale;
        private bool _isInitialized;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _baseColor = spriteRenderer.color;
            }
            
            _baseScale = transform.localScale;
            // Fallback in case it was zero during Awake for some reason
            if (_baseScale == Vector3.zero) _baseScale = Vector3.one;
        }

        public void Initialize(float damage)
        {
            _damage = damage;
            _isInitialized = true;
        }

        private void OnEnable()
        {
            // Ensure starting scale is fully reset on re-use from the pool
            transform.DOKill();
            transform.localScale = _baseScale;

            // Start shrinking immediately over the total lifetime + fade duration
            transform.DOScale(Vector3.zero, lifetime + fadeDuration).SetEase(Ease.Linear);

            if (spriteRenderer != null)
            {
                // Ensure starting alpha is fully reset
                spriteRenderer.DOKill();
                spriteRenderer.color = _baseColor;

                // Wait for the lifetime, then fade to 0 alpha, then deactivate
                spriteRenderer.DOFade(0f, fadeDuration)
                    .SetDelay(lifetime)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                // Fallback deactivation if there is no sprite renderer
                DOVirtual.DelayedCall(lifetime + fadeDuration, () => gameObject.SetActive(false));
            }
        }

        private void OnDisable()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.DOKill();
            }
            transform.DOKill();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_isInitialized) return;

            // Example hazard logic:
            // Replace with your actual Player health check or damage interface
            /*
            if (other.CompareTag("Player") && other.TryGetComponent(out PlayerHealth health))
            {
                health.TakeDamage(_damage);
            }
            */
        }
    }
}
