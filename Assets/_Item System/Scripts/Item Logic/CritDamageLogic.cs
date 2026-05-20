using UnityEngine;

public class CritDamageLogic : PlayerStatItemLogicBase, ICritDamageLogic
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackCritDamageBonus = 0.2f;
    [SerializeField] private float fallbackCritDamageBonusPerStack = 0.23f;

    protected override PlayerStatType StatType => PlayerStatType.CritDamage;
    protected override float FallbackBaseValue => fallbackCritDamageBonus;
    protected override float FallbackPerStackValue => fallbackCritDamageBonusPerStack;

    public float AddCritDamageBonus()
    {
        return FallbackValue;
    }
}
