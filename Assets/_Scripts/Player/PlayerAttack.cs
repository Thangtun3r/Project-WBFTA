using UnityEngine;
using System;

public enum AttackDetectionMethod
{
    Collision,
    Trigger
}

public class PlayerAttack : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private AttackDetectionMethod detectionMethod = AttackDetectionMethod.Collision;
    [SerializeField] private LayerMask targetLayers; // Only select layers that should take damage (e.g., "Enemy")

    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float attackGraceTime = 0.005f;
    
    private PlayerStatMachine _statMachine;
    private float _lastAttackTime = -Mathf.Infinity;

    // Updated Action to pass damage and crit status
    public event Action<Vector2, float, bool> OnHitTarget; 

    private void Awake()
    {
        // Search in parent in case this script is on a child "Hitbox" object
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
        // 1. Layer Check: This replaces the "Exclude" logic. 
        // If the object's layer is NOT in our Target Layers mask, we ignore it entirely.
        if (((1 << collider.gameObject.layer) & targetLayers) == 0) return;

        // 2. Cooldown/Grace Period Check
        if (Time.time - _lastAttackTime < attackGraceTime) return;

        // 3. Component Lookup
        var damagable = collider.GetComponent<IDamagable>() ?? 
                        collider.GetComponentInParent<IDamagable>();

        if (damagable != null)
        {
            // Get calculated damage from stat machine (includes crit)
            float finalDamage = _statMachine != null ? _statMachine.GetCalculatedAttackDamage() : damageAmount;
            bool isCrit = _statMachine != null ? _statMachine.WasLastAttackCrit() : false;
            
            damagable.TakeDamage(finalDamage);
            _lastAttackTime = Time.time;
            
            // Fire event for VFX/UI (Camera shake, damage numbers, etc.)
            OnHitTarget?.Invoke(hitPoint, finalDamage, isCrit);
        }
    }
    
    public void SetDamage(float newDamage) => damageAmount = newDamage;
}