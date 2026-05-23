using UnityEngine;

public abstract class ProcItemLogicBase : ItemLogicBase, IItemEventListener, ITriggerableItem
{
    private float _nextAllowedTriggerTime;

    protected virtual float ProcChance => GetProcChance(1f);
    protected virtual float Cooldown => GetCooldown(0f);

    protected override void OnInitialize()
    {
    }

    protected override void HandleStackChanged(int amountChanged)
    {
    }

    public override void Dispose()
    {
    }

    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (!TryBuildTriggerContext(itemEvent, out ItemTriggerContext triggerContext))
        {
            return;
        }

        Owner.RequestTrigger(triggerContext);
    }

    public virtual bool CanTrigger(ItemTriggerContext context)
    {
        if (!IsTriggerContextValid(context) || Time.time < _nextAllowedTriggerTime)
        {
            return false;
        }

        return RollProc(context);
    }

    public void Trigger(ItemTriggerContext context)
    {
        _nextAllowedTriggerTime = Time.time + Cooldown;
        ExecuteTrigger(context);
    }

    protected abstract bool TryBuildTriggerContext(ItemEvent itemEvent, out ItemTriggerContext triggerContext);
    protected abstract void ExecuteTrigger(ItemTriggerContext context);

    protected virtual bool RollProc(ItemTriggerContext context)
    {
        float coefficient = Mathf.Max(0f, context.ProcCoefficient);
        float finalChance = Mathf.Clamp01(ProcChance * coefficient);
        return Random.value <= finalChance;
    }

    protected virtual bool IsTriggerContextValid(ItemTriggerContext context)
    {
        return true;
    }

    protected float GetProcChance(float fallbackValue, float fallbackPerStack = 0f)
    {
        return Mathf.Clamp01(GetItemStat(ItemStatType.ProcChance, fallbackValue, fallbackPerStack));
    }

    protected float GetCooldown(float fallbackValue, float fallbackPerStack = 0f)
    {
        return GetItemStat(ItemStatType.Cooldown, fallbackValue, fallbackPerStack);
    }

    protected float GetDamageMultiplier(float fallbackValue, float fallbackPerStack = 0f)
    {
        return GetItemStat(ItemStatType.DamageMultiplier, fallbackValue, fallbackPerStack);
    }

    protected float GetItemStat(ItemStatType statType, float fallbackValue, float fallbackPerStack = 0f)
    {
        return Owner.GetItemStat(statType, GetStackedValue(fallbackValue, fallbackPerStack));
    }

    protected float GetParameter(string key, float fallbackValue, float fallbackPerStack = 0f)
    {
        return Owner.GetParameter(key, GetStackedValue(fallbackValue, fallbackPerStack));
    }

    protected float GetStackedValue(float baseValue, float perStackValue)
    {
        return baseValue + (Mathf.Max(Owner.StackSize, 1) - 1) * perStackValue;
    }
}

public abstract class ProcOnHitItemLogicBase : ProcItemLogicBase
{
    protected override bool TryBuildTriggerContext(ItemEvent itemEvent, out ItemTriggerContext triggerContext)
    {
        triggerContext = default;
        if (itemEvent.Type != ItemEventType.HitEnemy || itemEvent.Target == null)
        {
            return false;
        }

        triggerContext = new ItemTriggerContext
        {
            SourceItem = Owner,
            Owner = Owner.OwnerObject,
            Target = itemEvent.Target,
            Damage = itemEvent.Damage,
            ProcCoefficient = Mathf.Max(0f, itemEvent.ProcCoefficient),
            IsCrit = itemEvent.IsCrit
        };
        return true;
    }

    protected override bool IsTriggerContextValid(ItemTriggerContext context)
    {
        return context.Target != null;
    }
}

public abstract class ProcOnKillItemLogicBase : ProcItemLogicBase
{
    protected override bool TryBuildTriggerContext(ItemEvent itemEvent, out ItemTriggerContext triggerContext)
    {
        triggerContext = default;
        if (itemEvent.Type != ItemEventType.EnemyKilled || itemEvent.Enemy == null)
        {
            return false;
        }

        triggerContext = new ItemTriggerContext
        {
            SourceItem = Owner,
            Owner = Owner.OwnerObject,
            Origin = itemEvent.Enemy.transform.position,
            Damage = itemEvent.Damage,
            ProcCoefficient = Mathf.Max(0f, itemEvent.ProcCoefficient),
            IsCrit = itemEvent.IsCrit
        };
        return true;
    }
}
