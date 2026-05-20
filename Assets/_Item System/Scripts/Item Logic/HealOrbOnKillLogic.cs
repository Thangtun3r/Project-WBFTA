using UnityEngine;

public class HealOrbOnKillLogic : ProcOnKillItemLogicBase
{
    private const string HealAmountParameterKey = "HealOrb.HealAmount";

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackProcChance = 0.2f;
    [SerializeField] private float fallbackHealAmount = 0.1f;

    protected override float ProcChance => GetProcChance(fallbackProcChance);

    private float HealAmount => GetParameter(HealAmountParameterKey, fallbackHealAmount, fallbackHealAmount);

    protected override void ExecuteTrigger(ItemTriggerContext context)
    {
        WorldObjectSpawner.Instance.Spawn("HealOrb", context.Origin, HealAmount);
    }
}
