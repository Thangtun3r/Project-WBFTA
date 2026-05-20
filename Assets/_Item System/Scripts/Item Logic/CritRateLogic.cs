using UnityEngine;

public class CritRateLogic : PlayerStatItemLogicBase, ICritRateLogic
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackCritChanceBonus = 0.2f;
    [SerializeField] private float fallbackCritChanceBonusPerStack = 0.1f;

    protected override PlayerStatType StatType => PlayerStatType.CritChance;
    protected override float FallbackBaseValue => fallbackCritChanceBonus;
    protected override float FallbackPerStackValue => fallbackCritChanceBonusPerStack;

    public float AddCritRateBonus()
    {
        return FallbackValue;
    }
}
