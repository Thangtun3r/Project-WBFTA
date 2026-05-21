using System.Collections.Generic;
using UnityEngine;

public class ScaleStatsWithCurrentHealthLogic : AttachedItemHealthStatMultiplierLogicBase, IModifierParameterRequirements
{
    private static readonly string[] RequiredKeys =
    {
        ModifierParameterKeys.HealthScaleMinimumMultiplier,
        ModifierParameterKeys.HealthScaleMaximumMultiplier
    };

    public IReadOnlyList<string> RequiredParameterKeys => RequiredKeys;

    protected override float EvaluateMultiplier(float healthRatio)
    {
        float minimumMultiplier = Owner.GetParameter(ModifierParameterKeys.HealthScaleMinimumMultiplier, 0.1f);
        float maximumMultiplier = Owner.GetParameter(ModifierParameterKeys.HealthScaleMaximumMultiplier, 2f);
        return Mathf.Lerp(minimumMultiplier, maximumMultiplier, Mathf.Clamp01(healthRatio));
    }
}
