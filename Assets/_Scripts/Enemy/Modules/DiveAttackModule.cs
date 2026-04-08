using UnityEngine;

namespace _Scripts.Enemy.Modules
{
    public class DiveAttackModule : MonoBehaviour, IEnemyAttack
    {
        [Header("Hitbox Setup")]
        [SerializeField] private GameObject hitboxRoot;

        [Header("Attack Settings")]
        [SerializeField] private float damageCooldown = 0.5f;

        private EnemyConfig _config;
        private bool _isAttacking;
        private float _cooldownTimer;

        private void Awake()
        {
            _config = GetComponentInParent<BaseEnemy>()?.Config;

            if (hitboxRoot == null)
            {
                Debug.LogWarning("DiveAttackModule requires a hitbox root to be assigned.");
                return;
            }

            Collider2D[] hitboxes = hitboxRoot.GetComponentsInChildren<Collider2D>();
            if (hitboxes.Length == 0)
            {
                Debug.LogWarning("No Collider2D components found under the DiveAttackModule hitbox root.");
                return;
            }

            int damage = _config != null ? _config.damage : 10;

            foreach (var col in hitboxes)
            {
                HitboxProxy proxy = col.gameObject.AddComponent<HitboxProxy>();
                proxy.attackModule = this;
                proxy.damage = damage;
            }
        }

        public void SetAttackActive(bool active)
        {
            _isAttacking = active;
        }

        private void Update()
        {
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
    }
}
