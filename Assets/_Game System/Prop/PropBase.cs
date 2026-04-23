using UnityEngine;
using System;

    public abstract class PropBase : MonoBehaviour, IDamagable, IHealthObservable
    {
        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = 50f;
        [SerializeField] protected float healthIncreasePerLevel = 10f;

        protected int currentLevel = 1;
        protected float currentHealth;
        protected Collider2D _collider;

        public int CurrentLevel => currentLevel;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public event Action<float, float> OnHealthChanged;

        protected virtual void Awake()
        {
            _collider = GetComponent<Collider2D>();
            ResetStats();
        }

        protected virtual void ResetStats()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetLevel(int level)
        {
            currentLevel = level;
            maxHealth = maxHealth + (level - 1) * healthIncreasePerLevel;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public virtual void TakeDamage(float damage)
        {
            if (currentHealth <= 0f) return;
            currentHealth -= damage;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0)
            {
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            if (_collider != null) _collider.enabled = false;
            OnDeathBehavior();
            Destroy(gameObject);
        }

        protected abstract void OnDeathBehavior();

        public virtual Transform GetTransform() => transform;
    }
