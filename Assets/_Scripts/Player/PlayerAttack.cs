using UnityEngine;
using System;

public enum AttackDetectionMethod
{
    Collision,
    Trigger
}

    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private float attackGraceTime = 0.005f;
        [SerializeField] private AttackDetectionMethod detectionMethod = AttackDetectionMethod.Collision;

        // Updated Action to pass the damage value as well
        public event Action<Vector2, float> OnHitTarget; 

        private float _lastAttackTime = -Mathf.Infinity;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (detectionMethod != AttackDetectionMethod.Collision) return;
            if (collision == null || collision.collider == null) return;
            HandleHit(collision.collider, collision.contacts[0].point);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (detectionMethod != AttackDetectionMethod.Trigger) return;
            if (collision == null) return;
            HandleHit(collision, collision.transform.position);
        }

        private void HandleHit(Collider2D collider, Vector2 hitPoint)
        {
            if (Time.time - _lastAttackTime < attackGraceTime) return;

            var damagable = collider.GetComponent<IDamagable>() ?? 
                            collider.GetComponentInParent<IDamagable>();

            if (damagable != null)
            {
                damagable.TakeDamage(damageAmount);
                _lastAttackTime = Time.time;
                OnHitTarget?.Invoke(hitPoint, damageAmount);
            }
        }
        
        public void SetDamage(float newDamage) => damageAmount = newDamage;
    }
