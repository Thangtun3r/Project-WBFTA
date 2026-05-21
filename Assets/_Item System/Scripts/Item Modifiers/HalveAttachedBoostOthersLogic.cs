using System.Collections.Generic;

public class HalveAttachedBoostOthersLogic : ItemModifier, IItemStatProvider, IItemParameterProvider, IModifierParameterRequirements
{
    private static readonly string[] RequiredKeys =
    {
        ModifierParameterKeys.TradeoffAttachedMultiplier,
        ModifierParameterKeys.TradeoffOtherBaseMultiplier,
        ModifierParameterKeys.TradeoffOtherPerStackMultiplier
    };

    public IReadOnlyList<string> RequiredParameterKeys => RequiredKeys;

    public void ModifyItemStat(ref ItemStatQuery query)
    {
        Apply(ref query.Multiplier, query.Item);
    }

    public void ModifyItemParameter(ref ItemParameterQuery query)
    {
        Apply(ref query.Multiplier, query.Item);
    }

    private void Apply(ref float multiplier, ItemRuntime queriedItem)
    {
        if (Owner == null || Owner.AttachedItem == null || queriedItem == null)
        {
            return;
        }

        if (queriedItem == Owner.AttachedItem)
        {
            multiplier *= Owner.GetParameter(ModifierParameterKeys.TradeoffAttachedMultiplier, 0.5f);
            return;
        }

        float baseMultiplier = Owner.GetParameter(ModifierParameterKeys.TradeoffOtherBaseMultiplier, 1.5f);
        float perStack = Owner.GetParameter(ModifierParameterKeys.TradeoffOtherPerStackMultiplier, 0.1f);
        multiplier *= baseMultiplier + perStack * (Owner.AttachedItemStackSize - 1);
    }
}
