using UnityEngine;
using DG.Tweening;

namespace _Scripts.Enemy
{
    public class EnemyVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer enemyVisual;
        [SerializeField] private SpriteRenderer turretVisual;
        [SerializeField] private ParticleSystem deathParticles;

        [Header("Juice Settings")]
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float shakeStrength = 15f;
        [SerializeField] private Color damageColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;

        [Header("Scale Tween")]
        [SerializeField] private Transform scaleTarget;
        [SerializeField] private float showScaleDuration = 0.2f;
        [SerializeField] private float hideScaleDuration = 0.15f;
        [SerializeField] private Ease showScaleEase = Ease.OutBack;
        [SerializeField] private Ease hideScaleEase = Ease.InBack;

        private Color _baseColor = Color.white;
        private Color _turretBaseColor = Color.white;
        private Transform _scaleTarget;
        private bool _isInitialized = false;

        private Transform ScaleTarget => _scaleTarget != null ? _scaleTarget : enemyVisual != null ? enemyVisual.transform : transform;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            
            _scaleTarget = scaleTarget != null ? scaleTarget : enemyVisual != null ? enemyVisual.transform : transform;

            if (enemyVisual != null)
            {
                _baseColor = enemyVisual.color;
            }
            if (turretVisual != null)
            {
                _turretBaseColor = turretVisual.color;
            }
            _isInitialized = true;
        }

        private void OnEnable()
        {
            Initialize(); // Ensure _baseColor is captured before applying it
            
            if (ScaleTarget != null)
            {
                ScaleTarget.DOKill();
                ScaleTarget.localScale = Vector3.zero;
                ScaleTarget
                    .DOScale(new Vector3(1.1f, 1.1f, 1.1f), showScaleDuration)
                    .SetEase(showScaleEase);
            }

            if (enemyVisual != null)
            {
                // Reset visual state for pooling safety
                enemyVisual.DOKill();
                enemyVisual.color = _baseColor;
                enemyVisual.enabled = true;
                enemyVisual.transform.localRotation = Quaternion.identity;
            }

            if (turretVisual != null)
            {
                turretVisual.DOKill();
                turretVisual.color = _turretBaseColor;
                turretVisual.enabled = true;
                turretVisual.transform.localRotation = Quaternion.identity;
            }
        }

        public void PlayHitEffects()
        {
            if (enemyVisual != null)
            {
                // Kill existing tweens on the visual child to prevent stuttering
                enemyVisual.transform.DOKill(true); 
                enemyVisual.transform.localRotation = Quaternion.identity; 
                
                // Shake Rotation (Still applied to the visual child)
                enemyVisual.transform.DOShakeRotation(shakeDuration, new Vector3(0, 0, shakeStrength));

                // Color Flash
                enemyVisual.DOKill();
                enemyVisual.DOColor(damageColor, flashDuration)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() => enemyVisual.color = _baseColor);
            }

            if (turretVisual != null)
            {
                turretVisual.transform.DOKill(true); 
                turretVisual.transform.localRotation = Quaternion.identity; 
                
                turretVisual.transform.DOShakeRotation(shakeDuration, new Vector3(0, 0, shakeStrength));

                turretVisual.DOKill();
                turretVisual.DOColor(damageColor, flashDuration)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() => turretVisual.color = _turretBaseColor);
            }
        }

        public void PlayDeathEffects()
        {
            if (enemyVisual != null && ScaleTarget != null)
            {
                enemyVisual.DOKill();
                if (turretVisual != null) turretVisual.DOKill();
                ScaleTarget.DOKill();
                
                enemyVisual.enabled = true;
                if (turretVisual != null) turretVisual.enabled = true;
                
                ScaleTarget
                    .DOScale(Vector3.zero, hideScaleDuration)
                    .SetEase(hideScaleEase)
                    .OnComplete(() => 
                    {
                        if (enemyVisual != null) enemyVisual.enabled = false;
                        if (turretVisual != null) turretVisual.enabled = false;
                    });
            }

            if (deathParticles != null)
            {
                deathParticles.Play();
            }
        }

        public void HideVisual()
        {
            if (enemyVisual != null)
            {
                enemyVisual.DOKill();
                enemyVisual.enabled = false;
            }
            if (turretVisual != null)
            {
                turretVisual.DOKill();
                turretVisual.enabled = false;
            }
        }

        public void ShowVisual()
        {
            if (enemyVisual != null)
            {
                enemyVisual.enabled = true;
                // reset color to default if it was changed
                enemyVisual.color = _baseColor;
            }
            if (turretVisual != null)
            {
                turretVisual.enabled = true;
                turretVisual.color = _turretBaseColor;
            }
        }

        public void Flip(bool flipX)
        {
            if (enemyVisual != null)
            {
                enemyVisual.flipX = flipX;
            }
            if (turretVisual != null)
            {
                // Optionally flip the turret visual as well depending on your art setup
                // For instance: sometimes turrets spin freely and shouldn't flip
                turretVisual.flipY = flipX; // Or flipX depending on top-down vs side-scroller. Leaving as flipY or flipX may need testing based on aim alignment
            }
        }
    }
}