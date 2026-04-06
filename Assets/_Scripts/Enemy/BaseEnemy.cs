using UnityEngine;

namespace _Scripts.Enemy
{
    public abstract class BaseEnemy : MonoBehaviour, IDamagable
    {
        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = 100f;
        protected float currentHealth;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void TakeDamage(float damage)
        {
            currentHealth -= damage;
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public virtual void Die()
        {
            // You can add death particle instubs here later!
            Destroy(gameObject);
        }
    }
}