// ...existing code...
using UnityEngine;

namespace _Scripts.Enemy
{
    public abstract class BaseEnemy : MonoBehaviour, IDamagable
    {
        private float _health;

        public virtual void TakeDamage(float damage)
        {
            _health -= damage;
            if (_health <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            Destroy(gameObject);
        }
    }
}

// ...existing code...
