using UnityEngine;

namespace _Scripts.Enemy
{
    public class Projectile : MonoBehaviour
    {
        private int _damage;

        public void Initialize(int damage)
        {
            _damage = damage;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Assuming your player has a specific tag or component, for example "Player"
            if (collision.CompareTag("Player"))
            {
                collision.GetComponent<IDamagable>()?.TakeDamage(_damage);
                
                Debug.Log($"Projectile hit Player! Dealt {_damage} damage.");
                
                // Destroy on impact
                Destroy(gameObject);
            }
            // Optional: Destroy on wall collision
            // else if (collision.CompareTag("Environment")) Destroy(gameObject);
        }
    }
}
