using System.Diagnostics;
using UnityEngine;

public class ChainDamageLogic : ItemLogicBase
{
    public float procChance = 0.25f;
    public int baseMaxBounces = 3;
    public int extraBouncesPerStack = 1;
    public float baseRadius = 20f;
    public float radiusPerStack = 1f;
    public float damageMultiplier = 0.7f;

    public override void Dispose()
    {
        GlobalEventManager.Instance.HandleOnHit -= HandleHit;
    }

    protected override void OnInitialize()
    {
        GlobalEventManager.Instance.HandleOnHit += HandleHit;
    }

    private void HandleHit(GameObject attacker, IDamagable target, float damage, bool isCrit)
    {


        if (UnityEngine.Random.value > procChance) return;

        int stackCount = UnityEngine.Mathf.Max(Owner.StackSize, 1);
        int maxBounces = baseMaxBounces + ((stackCount - 1) * extraBouncesPerStack);
        float searchRadius = baseRadius + ((stackCount - 1) * radiusPerStack);

        var nearby = CurrentEnemyRegistry.Instance.GetTargetsInRadius(
            target.GetTransform().position,
            searchRadius,
            maxBounces
        );

        foreach (var enemy in nearby)
        {
            if (enemy == null || enemy.gameObject == null || enemy.gameObject == target.GetTransform().gameObject) continue;

            float chainDamage = damage * damageMultiplier;
            enemy.TakeDamage(chainDamage);
            
            if (FloatingDamagePool.Instance != null)
            {
                FloatingDamagePool.Instance.SpawnDamage(enemy.transform.position, chainDamage, isCrit);
            }

            EffectContext context = new EffectContext {
                Origin = target.GetTransform().position,
                Destination = enemy.transform.position,
                Radius = searchRadius
            };

            Owner.TriggerEffect(context);
        }
    }
}