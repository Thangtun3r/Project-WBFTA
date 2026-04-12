using UnityEngine;

namespace _Scripts.Enemy
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 5f; // Destroy projectile after this many seconds
        
        private float _damage;
        private float _spawnTime;

        private void Start()
        {
            _spawnTime = Time.time;
        }

        public void Initialize(float damage)
        {
            _damage = damage;
        }

        private void Update()
        {
            // Check if projectile has exceeded its lifetime
            if (Time.time - _spawnTime >= lifeTime)
            {
                Destroy(gameObject);
            }
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
