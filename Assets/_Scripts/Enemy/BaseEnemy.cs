using UnityEngine;
using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public abstract class BaseEnemy : MonoBehaviour, IDamagable, IHealthObservable
    {
        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected EnemyConfig config;
        [SerializeField] protected float deathReturnDelay = 3f;
        
        protected int currentLevel;
        protected float currentDamage;
        protected float currentHealth;
        protected EnemyVisuals _visuals;
        protected EnemyFSM _fsm;
        protected Collider2D _collider;
        
        public EnemyConfig Config => config;
        public int CurrentLevel => currentLevel;
        public float CurrentDamage => currentDamage;
        
        // IHealthObservable implementation
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public event System.Action<float, float> OnHealthChanged;

        protected virtual void Awake()
        {
            if (config != null)
            {
                currentLevel = config.level;
                currentDamage = config.damage;
                maxHealth = config.maxHealth;
            }

            currentHealth = maxHealth;
            _visuals = GetComponentInChildren<EnemyVisuals>();
            _fsm = GetComponentInChildren<EnemyFSM>();
            _collider = GetComponent<Collider2D>();
            
            // Notify observers of initial health state
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetLevel(int level)
        {
            if (config == null) return;

            currentLevel = level;

            // Apply health scaling
            maxHealth = config.maxHealth + (level - 1) * config.healthIncreasePerLevel;
            currentHealth = maxHealth;

            // Apply damage scaling
            currentDamage = config.damage + (level - 1) * config.damageIncreasePerLevel;

            // Notify observers of health change
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"Enemy leveled to {level}! Health: {maxHealth}, Damage: {currentDamage}");
        }

        public virtual void TakeDamage(float damage)
        {
            if (currentHealth <= 0f) return;

            currentHealth -= damage;
            _visuals?.PlayHitEffects();
            
            // Notify observers of health change
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                OnDeath();
            }
        }

        public virtual void Die()
        {
            // Optional subclass-specific death log or drop logic
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
            
            Die();
            
            Invoke(nameof(DeactivateObject), deathReturnDelay);
        }

        protected virtual void DeactivateObject()
        {
            gameObject.SetActive(false);
        }
    }
}