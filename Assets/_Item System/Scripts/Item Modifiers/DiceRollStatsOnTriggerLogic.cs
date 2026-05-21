using System.Collections.Generic;
using UnityEngine;

public class DiceRollStatsOnTriggerLogic : ItemModifier, IItemTriggerPreprocessor, IItemStatProvider, IItemParameterProvider, IModifierParameterRequirements
{
    private static readonly string[] RequiredKeys =
    {
        ModifierParameterKeys.DiceOddMultiplier,
        ModifierParameterKeys.DiceEvenMultiplier
    };

    private float _currentMultiplier = 1f;

    public IReadOnlyList<string> RequiredParameterKeys => RequiredKeys;

    public void BeforeItemTrigger(ref ItemTriggerContext context)
    {
        if (Owner == null || Owner.AttachedItem == null)
        {
            return;
        }

        int roll = Random.Range(1, 7);
        _currentMultiplier = roll % 2 == 0
            ? Owner.GetParameter(ModifierParameterKeys.DiceEvenMultiplier, 1.5f)
            : Owner.GetParameter(ModifierParameterKeys.DiceOddMultiplier, 0.5f);
    }

    public void ModifyItemStat(ref ItemStatQuery query)
    {
        if (Owner != null && query.Item == Owner.AttachedItem)
        {
            query.Multiplier *= _currentMultiplier;
        }
    }

    public void ModifyItemParameter(ref ItemParameterQuery query)
    {
        if (Owner != null && query.Item == Owner.AttachedItem)
        {
            query.Multiplier *= _currentMultiplier;
        }
    }
}
