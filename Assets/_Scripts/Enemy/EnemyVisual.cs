using UnityEngine;
using DG.Tweening;

namespace _Scripts.Enemy
{
    public class EnemyVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer enemyVisual;
        [SerializeField] private ParticleSystem deathParticles;

        [Header("Juice Settings")]
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float shakeStrength = 15f;
        [SerializeField] private Color damageColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private float spawnScaleDuration = 0.5f;

        private Color _baseColor;
        private Vector3 _defaultScale;

        private void Awake()
        {
            if (enemyVisual != null)
            {
                // Register the default size and color exactly as they are set in the Inspector
                _defaultScale = enemyVisual.transform.localScale;
                _baseColor = enemyVisual.color;
            }
        }

        private void OnEnable()
        {
            if (enemyVisual != null)
            {
                // Reset visual state for pooling safety
                enemyVisual.enabled = true;
                enemyVisual.transform.DOKill();
                enemyVisual.color = _baseColor;
                
                // 1. Set scale to zero immediately
                enemyVisual.transform.localScale = Vector3.zero;

                // 2. Scale up to the registered default size
                // SetUpdate(true) ensures this plays even if the game is paused
                enemyVisual.transform.DOScale(_defaultScale, spawnScaleDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
        }

        public void PlayHitEffects()
        {
            if (enemyVisual == null) return;

            // Kill existing tweens to prevent stuttering during rapid hits
            enemyVisual.transform.DOKill(true); 
            enemyVisual.transform.localRotation = Quaternion.identity; 
            
            // Shake Rotation
            enemyVisual.transform.DOShakeRotation(shakeDuration, new Vector3(0, 0, shakeStrength));

            // Color Flash (Yoyo loops from base to damage color and back)
            enemyVisual.DOColor(damageColor, flashDuration)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => enemyVisual.color = _baseColor);
        }

        public void PlayDeathEffects()
        {
            if (deathParticles != null)
            {
                deathParticles.Play();
            }
        }

        public void HideVisual()
        {
            if (enemyVisual != null)
            {
                enemyVisual.enabled = false;
            }
        }

        public void Flip(bool flipX)
        {
            if (enemyVisual != null)
            {
                enemyVisual.flipX = flipX;
            }
        }
    }
}