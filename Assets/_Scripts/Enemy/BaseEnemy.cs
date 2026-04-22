using UnityEngine;
using _Scripts.Enemy.Modules;
using System;

namespace _Scripts.Enemy
{
    public abstract class BaseEnemy : MonoBehaviour, IDamagable, IHealthObservable
    {
        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected EnemyConfig config;
        [SerializeField] protected float deathReturnDelay = 3f;

        // NEW: The "Magic Link" to the pool
        [HideInInspector] public GameObject sourcePrefab;
        
        protected int currentLevel;
        protected float currentDamage;
        protected float currentHealth;
        protected EnemyVisuals _visuals;
        protected EnemyFSM _fsm;
        protected Collider2D _collider;
        
        public EnemyConfig Config => config;
        public int CurrentLevel => currentLevel;
        public float CurrentDamage => currentDamage;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public event System.Action<float, float> OnHealthChanged;

        protected virtual void Awake()
        {
            _visuals = GetComponentInChildren<EnemyVisuals>();
            _fsm = GetComponentInChildren<EnemyFSM>();
            _collider = GetComponent<Collider2D>();
            
            // Initial setup logic
            ResetStats();
        }

        void OnEnable()
        {
            CurrentEnemyRegistry.Instance?.Register(this);
            // RESET: Ensures the enemy is alive and visible when pulled from the pool
            ResetEnemyState();
        }

        void OnDisable()
        {
            CurrentEnemyRegistry.Instance?.Unregister(this);
            CancelInvoke(); // Safety: stops DeactivateObject if manually disabled
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
            if (_collider != null) _collider.enabled = true;
            if (_visuals != null) _visuals.ShowVisual();
            // Optional: Reset FSM to Idle if needed
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
            _visuals?.PlayHitEffects();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0) OnDeath();
        }

        protected virtual void OnDeath()
        {
            if (_collider != null) _collider.enabled = false;
            if (_visuals != null)
            {
                _visuals.PlayDeathEffects();
                _visuals.HideVisual();
            }

            _fsm?.QueueNextState(EnemyFSM.EnemyState.Dead);
            RewardPlayer(config.tier, currentLevel);
            Die();
            
            Invoke(nameof(DeactivateObject), deathReturnDelay);
        }

        protected virtual void DeactivateObject()
        {
            // NEW: Instead of just setting active false, we return it to the pool
            if (sourcePrefab != null && EnemyPoolManager.Instance != null)
                EnemyPoolManager.Instance.Return(sourcePrefab, gameObject);
            else
                gameObject.SetActive(false);
        }

        // Stubs for your logic
        public virtual Transform GetTransform() => transform;
        public virtual void Die() { }
        protected virtual void RewardPlayer(int tier, int level) => EnemyRewardManager.Instance?.HandleReward(tier, level);
    }
}