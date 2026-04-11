using UnityEngine;

namespace _Scripts.Enemy.Modules
{
    public class SpinAttackModule : MonoBehaviour, IEnemyAttack
    {
        [Header("Spin Settings")]
        [SerializeField] private Transform spinningRoot;
        [SerializeField] private float spinSpeed = 360f; // Degrees per second
        [SerializeField] private bool alwaysSpin = false;
        
        [Header("Damage Settings")]
        [SerializeField] private float damageCooldown = 0.5f; // Prevent hitting every frame
        
        private EnemyConfig _config;
        private bool _isAttacking;
        
        private float _cooldownTimer;

        private void Awake()
        {
            _config = GetComponentInParent<BaseEnemy>()?.Config;
            float spinDamage = _config != null ? _config.damage : 10f;
            
            if (spinningRoot != null)
            {
                // Get all colliders in the spinning root to use as hitboxes
                Collider2D[] hitboxes = spinningRoot.GetComponentsInChildren<Collider2D>();
                
                foreach (var col in hitboxes)
                {
                    // Dynamically attach the proxy to route collisions to this script
                    HitboxProxy proxy = col.gameObject.AddComponent<HitboxProxy>();
                    proxy.attackModule = this;
                    proxy.damage = spinDamage;
                }
            }
            else
            {
                Debug.LogWarning("Spinning Root not assigned on SpinAttackModule!");
            }
        }

        public void SetAttackActive(bool active)
        {
            _isAttacking = active;
        }

        private void Update()
        {
            // Spin regardless, or only spin when attacking based on the toggle
            if (_isAttacking || alwaysSpin)
            {
                Spin();
            }

            // Cooldown countdown
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        public bool CanHit()
        {
            return _isAttacking && _cooldownTimer <= 0;
        }

        public void StartHitCooldown()
        {
            if (!_isAttacking) return;
            
            _cooldownTimer = damageCooldown;
        }

        private void Spin()
        {
            if (spinningRoot != null)
            {
                // Simple 2D rotation on the Z axis
                spinningRoot.Rotate(0, 0, spinSpeed * Time.deltaTime);
            }
        }
    }
}