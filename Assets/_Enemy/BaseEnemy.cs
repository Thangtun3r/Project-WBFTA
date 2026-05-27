using UnityEngine;
using _Scripts.Enemy.Modules;
using System;

namespace _Scripts.Enemy
{
    public abstract class BaseEnemy : MonoBehaviour, IDamagable, IHealthObservable
    {
        public static event Action<BaseEnemy> OnEnemyDeath;
        public static event Action<BaseEnemy> OnEnemyKilled;
        public static event Action<BaseEnemy> OnAnyEnemyHit;
        public event Action OnEnemyHit;
        public event Action OnEnemyDeath_Local;

        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected EnemyConfig config;
        protected float deathReturnDelay = 0f;

        // NEW: The "Magic Link" to the pool
        [HideInInspector] public GameObject sourcePrefab;
        
        protected int currentLevel;
        protected float currentDamage;
        protected float currentHealth;
        protected EnemyVisual _visuals;
        protected EnemyFSM _fsm;
        protected Collider2D _collider;
        protected EnemyStatusController _status;
        
        public EnemyConfig Config => config;
        public int CurrentLevel => currentLevel;
        public float CurrentDamage => currentDamage;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public event System.Action<float, float> OnHealthChanged;

        protected virtual void Awake()
        {
            _visuals = GetComponentInChildren<EnemyVisual>();
            _fsm = GetComponentInChildren<EnemyFSM>();
            _collider = GetComponent<Collider2D>();
            _status = GetComponent<EnemyStatusController>() ?? gameObject.AddComponent<EnemyStatusController>();
            
            // Initial setup logic
            ResetStats();
        }

        void OnEnable()
        {
            CurrentEnemyRegistry.Instance?.Register(this);
            ResetEnemyState();
        }

        void OnDisable()
        {
            CurrentEnemyRegistry.Instance?.Unregister(this);
            CancelInvoke();
        }

        private void ResetStats()
        {
            if (config != null)
            {
                currentLevel = config.level;
                currentDamage = config.damage;
                maxHealth = config.maxHealth;
            }
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        protected virtual void ResetEnemyState()
        {
            ResetStats();
            _status?.ClearStatuses();
            if (_collider != null) _collider.enabled = true;
            // Reset FSM to Patrol when pulled from the pool
            if (_fsm != null)
            {
                _fsm.enabled = true;
                _fsm.ForceState(EnemyFSM.EnemyState.Patrol, true);
            }
        }

        public void SetLevel(int level)
        {
            if (config == null) return;
            currentLevel = level;
            maxHealth = config.maxHealth + (level - 1) * config.healthIncreasePerLevel;
            currentHealth = maxHealth;
            currentDamage = config.damage + (level - 1) * config.damageIncreasePerLevel;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public virtual void TakeDamage(float damage)
        {
            if (currentHealth <= 0f) return;
            currentHealth -= damage;
            
            // Broadcast hit events
            OnEnemyHit?.Invoke();
            OnAnyEnemyHit?.Invoke(this);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0) OnDeath();
        }

        // protected virtual void EndHitStun()
        // {
        //     if (currentHealth > 0f && _fsm != null)
        //     {
        //         _fsm.enabled = true;
        //     }
        // }

        protected virtual void OnDeath()
        {
            if (_collider != null) _collider.enabled = false;
            
            OnEnemyDeath_Local?.Invoke();
            _fsm?.ForceState(EnemyFSM.EnemyState.Dead);
            _status?.ApplyStatus(EnemyStatusType.Dead);
            OnEnemyDeath?.Invoke(this);
            OnEnemyKilled?.Invoke(this);
            Invoke(nameof(DeactivateObject), deathReturnDelay);
        }

        protected virtual void DeactivateObject()
        {
        
            if (sourcePrefab != null && EnemyPoolManager.Instance != null)
                EnemyPoolManager.Instance.Return(sourcePrefab, gameObject);
            else
                gameObject.SetActive(false);
        }

        public virtual Transform GetTransform() => transform;
    }
}
