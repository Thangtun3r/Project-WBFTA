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
        
        private PlayerStatMachine _statMachine;

        // Updated Action to pass damage and crit status
        public event Action<Vector2, float, bool> OnHitTarget; 

        private float _lastAttackTime = -Mathf.Infinity;

        private void Awake()
        {
            _statMachine = GetComponentInParent<PlayerStatMachine>();
        }

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
                // Get calculated damage from stat machine (includes crit)
                float finalDamage = _statMachine != null ? _statMachine.GetCalculatedAttackDamage() : damageAmount;
                bool isCrit = _statMachine != null ? _statMachine.WasLastAttackCrit() : false;
                
                damagable.TakeDamage(finalDamage);
                _lastAttackTime = Time.time;
                OnHitTarget?.Invoke(hitPoint, finalDamage, isCrit);
            }
        }
        
        public void SetDamage(float newDamage) => damageAmount = newDamage;
    }
