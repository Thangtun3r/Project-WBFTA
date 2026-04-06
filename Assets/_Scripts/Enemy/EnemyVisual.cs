using UnityEngine;
using DG.Tweening;

namespace _Scripts.Enemy
{
    public class EnemyVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer enemyVisual;
        [SerializeField] private ParticleSystem deathParticles; // Reference to the attached child!

        [Header("Juice Settings")]
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float shakeStrength = 15f;
        [SerializeField] private Color damageColor = Color.red;
        [SerializeField] private float flashDuration = 0.1f;

        private Color _baseColor;

        private void Start()
        {
            if (enemyVisual != null)
                _baseColor = enemyVisual.color;
        }

        public void PlayHitEffects()
        {
            if (enemyVisual == null) return;

            enemyVisual.transform.DOKill(true); 
            enemyVisual.transform.localRotation = Quaternion.identity; 
            enemyVisual.transform.DOShakeRotation(shakeDuration, new Vector3(0, 0, shakeStrength));

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
                enemyVisual.enabled = false; // Just turn off the sprite renderer!
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