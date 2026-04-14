using UnityEngine;

    public class Projectile : MonoBehaviour
    {
        private float _damage;
        private Rigidbody2D _rb;
        private System.Action<Projectile> _returnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void Launch(float damage, Vector2 velocity, System.Action<Projectile> returnAction)
        {
            _damage = damage;
            _rb.linearVelocity = velocity;
            _returnToPool = returnAction;

            // Auto-return to pool after 5 seconds if it hits nothing
            CancelInvoke(); 
            Invoke(nameof(Deactivate), 5f);
        }

        private void Deactivate()
        {
            _returnToPool?.Invoke(this);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out IDamagable target))
            {
                target.TakeDamage(_damage);
                Deactivate();
            }
            // Add environment check here if needed
        }
    }