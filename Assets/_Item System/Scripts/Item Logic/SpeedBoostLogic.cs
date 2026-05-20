using UnityEngine;

public class SpeedBoostLogic : PlayerStatItemLogicBase
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackMoveSpeedMultiplierBonus = 0.14f;
    [SerializeField] private float fallbackMoveSpeedMultiplierBonusPerStack = 0.14f;

    protected override PlayerStatType StatType => PlayerStatType.MoveSpeedMultiplier;
    protected override float FallbackBaseValue => fallbackMoveSpeedMultiplierBonus;
    protected override float FallbackPerStackValue => fallbackMoveSpeedMultiplierBonusPerStack;

    public float GetBonusMultiplier()
    {
        return FallbackValue;
    }
}
