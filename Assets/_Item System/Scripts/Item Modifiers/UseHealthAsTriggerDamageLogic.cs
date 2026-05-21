using System.Collections.Generic;
using UnityEngine;

public class UseHealthAsTriggerDamageLogic : ItemModifier, IItemTriggerPreprocessor, IModifierParameterRequirements
{
    private static readonly string[] RequiredKeys =
    {
        ModifierParameterKeys.HealthDamageDivisor,
        ModifierParameterKeys.HealthDamageUseMaxHealth
    };

    public IReadOnlyList<string> RequiredParameterKeys => RequiredKeys;

    public void BeforeItemTrigger(ref ItemTriggerContext context)
    {
        if (Owner == null || Owner.AttachedItem == null)
        {
            return;
        }

        PlayerHealth playerHealth = context.Owner != null
            ? context.Owner.GetComponent<PlayerHealth>() ?? context.Owner.GetComponentInParent<PlayerHealth>()
            : null;
        if (playerHealth == null)
        {
            return;
        }

        float divisor = Mathf.Max(0.001f, Owner.GetParameter(ModifierParameterKeys.HealthDamageDivisor, 10f));
        bool useMaxHealth = Owner.GetParameter(ModifierParameterKeys.HealthDamageUseMaxHealth, 0f) > 0f;
        float healthBase = useMaxHealth ? playerHealth.MaxHealth : playerHealth.CurrentHealth;
        context.Damage = healthBase / divisor;
    }
}
