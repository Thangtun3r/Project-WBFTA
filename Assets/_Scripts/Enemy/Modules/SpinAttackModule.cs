using UnityEngine;
using System.Collections.Generic;

namespace _Scripts.Enemy.Modules
{
    public class SpinAttackModule : MonoBehaviour, IEnemyAttack
    {
        [Header("Spin Settings")]
        [SerializeField] private Transform spinningRoot;
        [SerializeField] private float spinSpeed = 360f;
        [SerializeField] private bool alwaysSpin = false;
        
        [Header("Damage Settings")]
        [SerializeField] private float damageCooldown = 0.5f;
        [SerializeField] private LayerMask targetLayer; // Set to "Player"
        [SerializeField] private float detectionRadius = 1f;

        private EnemyConfig _config;
        private bool _isAttacking;
        private float _cooldownTimer;
        private float _spinDamage;

        // List to keep track of proxies we've created
        private List<HitboxProxy> _proxies = new List<HitboxProxy>();

        private void Awake()
        {
            _config = GetComponentInParent<BaseEnemy>()?.Config;
            _spinDamage = _config != null ? _config.damage : 10f;
            
            SetupProxies();
        }

        private void SetupProxies()
        {
            if (spinningRoot == null) return;

            Collider2D[] hitboxes = spinningRoot.GetComponentsInChildren<Collider2D>();
            foreach (var col in hitboxes)
            {
                HitboxProxy proxy = col.gameObject.GetComponent<HitboxProxy>() ?? col.gameObject.AddComponent<HitboxProxy>();
                
                proxy.attackModule = this;
                proxy.damage = _spinDamage;
                
                _proxies.Add(proxy);
            }
        }

        private void Update()
        {
            if (_isAttacking || alwaysSpin)
            {
                Spin();
            }

            if (_cooldownTimer > 0) _cooldownTimer -= Time.deltaTime;
        }

        private void Spin()
        {
            if (spinningRoot != null)
            {
                // Rotation via Transform is fine for visuals/colliders 
                // IF we aren't relying on high-speed physics tunneling
                spinningRoot.Rotate(0, 0, spinSpeed * Time.deltaTime);
            }
        }

        public bool CanHit() => _isAttacking && _cooldownTimer <= 0;

        public void StartHitCooldown() => _cooldownTimer = damageCooldown;

        public void SetAttackActive(bool active) => _isAttacking = active;
    }
}