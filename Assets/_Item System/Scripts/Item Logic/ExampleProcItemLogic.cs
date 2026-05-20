using UnityEngine;

// Reference template only.
// Do not add this exact class to ItemDatabaseFactory as gameplay logic.
//
// Use this file as a pattern when creating a proc item:
// - ProcOnHitItemLogicBase listens for HitEnemy and routes it through Owner.RequestTrigger.
// - ProcOnKillItemLogicBase does the same for EnemyKilled.
// - Inherit ProcItemLogicBase directly only when the item needs a custom event source.
// - Override ExecuteTrigger for the unique item effect.
public class ExampleProcItemLogic : ProcOnHitItemLogicBase
{
    // Fallback values keep old/simple items working even if the ItemDefinition has no itemStats.
    // Final values should be read through base helpers so ItemDefinition data and modifiers can change them.
    private const float FallbackProcChance = 0.25f;
    private const float FallbackDamageMultiplier = 1f;

    // ProcChance is checked by ProcItemLogicBase before ExecuteTrigger runs.
    // GetProcChance reads ItemDefinition.itemStats first, then this fallback, then modifiers/providers.
    protected override float ProcChance => GetProcChance(FallbackProcChance);

    protected override void ExecuteTrigger(ItemTriggerContext context)
    {
        // ExecuteTrigger is the actual effect execution.
        // Keep tuning values query-based so modifiers can alter them without touching this class.
        float damageMultiplier = GetDamageMultiplier(FallbackDamageMultiplier);
        float finalDamage = context.Damage * damageMultiplier;

        context.Target.TakeDamage(finalDamage);

        // Visual effects stay separate from gameplay.
        // Owner.TriggerEffect instantiates ItemDefinition.effectPrefab and passes EffectContext to IItemEffect.
        Owner.TriggerEffect(new EffectContext
        {
            Origin = context.Target.GetTransform().position,
            Destination = context.Target.GetTransform().position,
            Radius = GetItemStat(ItemStatType.Radius, 0f)
        });
    }
}
