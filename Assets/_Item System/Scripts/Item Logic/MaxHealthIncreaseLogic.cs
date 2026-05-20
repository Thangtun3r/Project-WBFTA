using UnityEngine;

public class MaxHealthIncreaseLogic : PlayerStatItemLogicBase
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackMaxHealthBonus = 25f;
    [SerializeField] private float fallbackMaxHealthBonusPerStack = 25f;

    protected override PlayerStatType StatType => PlayerStatType.MaxHealth;
    protected override float FallbackBaseValue => fallbackMaxHealthBonus;
    protected override float FallbackPerStackValue => fallbackMaxHealthBonusPerStack;

    public float GetTotalBonus()
    {
        return FallbackValue;
    }
}
