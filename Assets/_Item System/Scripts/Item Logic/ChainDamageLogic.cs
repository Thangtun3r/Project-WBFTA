using UnityEngine;

public class ChainDamageLogic : ProcOnHitItemLogicBase
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackProcChance = 0.25f;
    [SerializeField] private int fallbackBaseBounceCount = 3;
    [SerializeField] private int fallbackBounceCountPerStack = 1;
    [SerializeField] private float fallbackBaseRadius = 20f;
    [SerializeField] private float fallbackRadiusPerStack = 1f;
    [SerializeField] private float fallbackDamageMultiplier = 0.7f;

    protected override float ProcChance => GetProcChance(fallbackProcChance);

    private int BounceCount => Mathf.Max(0, Mathf.RoundToInt(
        GetItemStat(ItemStatType.BounceCount, fallbackBaseBounceCount, fallbackBounceCountPerStack)));

    private float Radius => GetItemStat(
        ItemStatType.Radius,
        fallbackBaseRadius,
        fallbackRadiusPerStack);

    private float DamageMultiplier => GetDamageMultiplier(fallbackDamageMultiplier);

    protected override void ExecuteTrigger(ItemTriggerContext context)
    {
        ApplyChainDamage(context.Target, context.Damage, context.IsCrit);
    }

    private void ApplyChainDamage(IDamagable target, float damage, bool isCrit)
    {
        Transform targetTransform = target.GetTransform();
        float radius = Radius;
        float chainDamage = damage * DamageMultiplier;

        var nearby = CurrentEnemyRegistry.Instance.GetTargetsInRadius(
            targetTransform.position,
            radius,
            BounceCount
        );

        foreach (var enemy in nearby)
        {
            if (enemy == null || enemy.gameObject == null || enemy.gameObject == targetTransform.gameObject)
            {
                continue;
            }

            enemy.TakeDamage(chainDamage);
            
            if (FloatingDamagePool.Instance != null)
            {
                FloatingDamagePool.Instance.SpawnDamage(enemy.transform.position, chainDamage, isCrit);
            }

            EffectContext context = new EffectContext {
                Origin = targetTransform.position,
                Destination = enemy.transform.position,
                Radius = radius
            };

            Owner.TriggerEffect(context);
        }
    }
}
