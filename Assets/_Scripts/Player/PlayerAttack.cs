using UnityEngine;
using System;


    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private float damageAmount = 10f;
        
        // Updated Action to pass the damage value as well
        public event Action<Vector2, float> OnHitTarget; 

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;
            
            var damagable = collision.collider.GetComponent<IDamagable>() ?? 
                            collision.collider.GetComponentInParent<IDamagable>();
            
            if (damagable != null) 
            {
                damagable.TakeDamage(damageAmount);
                
                // Your original contact point logic
                Vector2 contactPoint = collision.contacts[0].point;
                
                // Shout the event with the position AND the damage amount
                OnHitTarget?.Invoke(contactPoint, damageAmount);
            }
        }
        
        public void SetDamage(float newDamage) => damageAmount = newDamage;
    }
