using System.Collections.Generic;

public class DoubleStatsBelowThresholdLogic : AttachedItemHealthStatMultiplierLogicBase, IModifierParameterRequirements
{
    private static readonly string[] RequiredKeys =
    {
        ModifierParameterKeys.LowHealthThreshold,
        ModifierParameterKeys.LowHealthMultiplier
    };

    public IReadOnlyList<string> RequiredParameterKeys => RequiredKeys;

    protected override float EvaluateMultiplier(float healthRatio)
    {
        float healthThreshold = Owner.GetParameter(ModifierParameterKeys.LowHealthThreshold, 0.1f);
        float multiplier = Owner.GetParameter(ModifierParameterKeys.LowHealthMultiplier, 2f);
        return healthRatio <= healthThreshold ? multiplier : 1f;
    }
}
