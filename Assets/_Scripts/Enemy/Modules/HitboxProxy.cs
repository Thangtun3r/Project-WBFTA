using UnityEngine;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    // Simply forwarding physics triggers or collisions to the main attack module
    public class HitboxProxy : MonoBehaviour
    {
        [HideInInspector] public IEnemyAttack attackModule;
        [HideInInspector] public float damage;
        [HideInInspector] public bool useTrigger = true;

        private Collider2D _col;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            if (_col != null)
            {
                _col.isTrigger = useTrigger;
            }
            
            // A trigger event is only sent to the child if it has its own Rigidbody2D.
            // We add a Kinematic one so it follows the parent's movement without falling.
            Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!useTrigger) return;
            HandleTrigger(collision);
        }

        private void HandleTrigger(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            if (attackModule == null || !attackModule.CanHit()) return;

            collision.GetComponentInParent<IDamagable>()?.TakeDamage(damage);
            Debug.Log($"Hitbox proxy hit Player for {damage} damage!");
            attackModule.StartHitCooldown();
        }
    }
}