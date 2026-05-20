using UnityEngine;

public abstract class PlayerStatItemLogicBase : ItemLogicBase, IPlayerStatProvider
{
    protected abstract PlayerStatType StatType { get; }
    protected abstract float FallbackBaseValue { get; }
    protected virtual float FallbackPerStackValue => 0f;

    protected float FallbackValue => GetStackedValue(FallbackBaseValue, FallbackPerStackValue);

    protected override void OnInitialize()
    {
    }

    protected override void HandleStackChanged(int amountChanged)
    {
    }

    public override void Dispose()
    {
    }

    public void ModifyPlayerStat(ref PlayerStatQuery query)
    {
        if (query.StatType != StatType || HasDefinitionValue())
        {
            return;
        }

        query.FlatBonus += FallbackValue;
    }

    protected float GetStackedValue(float baseValue, float perStackValue)
    {
        return baseValue + (Mathf.Max(Owner.StackSize, 1) - 1) * perStackValue;
    }

    private bool HasDefinitionValue()
    {
        return Owner.Definition != null &&
               ItemStatUtility.TryGetPlayerStat(Owner.Definition.playerStats, StatType, Owner.StackSize, out _);
    }
}
