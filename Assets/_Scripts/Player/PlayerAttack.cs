using UnityEngine;
using System;

public class PlayerAttack : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 10f;
    
    private PlayerStatMachine _statMachine;

    public event Action<Vector2, float, bool> OnHitTarget; 

    private void Awake()
    {
        _statMachine = GetComponentInParent<PlayerStatMachine>();
    }

    public bool DealDamage(
        IDamagable target,
        Vector2 hitPoint,
        float damageMultiplier = 1f,
        float procCoefficient = 1f,
        GameObject source = null)
    {
        if (target == null)
        {
            return false;
        }

        float baseDamage = _statMachine != null ? _statMachine.GetCalculatedAttackDamage() : damageAmount;
        bool isCrit = _statMachine != null && _statMachine.WasLastAttackCrit();
        float finalDamage = baseDamage * Mathf.Max(0f, damageMultiplier);
        float finalProcCoefficient = Mathf.Max(0f, procCoefficient);

        target.TakeDamage(finalDamage);

        OnHitTarget?.Invoke(hitPoint, finalDamage, isCrit);
        FloatingDamagePool.Instance?.SpawnDamage(hitPoint, finalDamage, isCrit);
        GlobalEventManager.Instance?.OnHit(source != null ? source : gameObject, target, finalDamage, isCrit, finalProcCoefficient);

        return true;
    }
        
    public void SetDamage(float newDamage) => damageAmount = newDamage;
}
