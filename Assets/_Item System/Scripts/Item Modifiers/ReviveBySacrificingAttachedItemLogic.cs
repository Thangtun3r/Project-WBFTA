using System.Collections.Generic;
using UnityEngine;

public class ReviveBySacrificingAttachedItemLogic : ItemModifier, IPlayerDeathHandler, IModifierParameterRequirements
{
    private static readonly string[] RequiredKeys =
    {
        ModifierParameterKeys.ReviveBaseHealthPercent,
        ModifierParameterKeys.RevivePerStackHealthPercent,
        ModifierParameterKeys.ReviveInvincibilityDuration
    };

    public IReadOnlyList<string> RequiredParameterKeys => RequiredKeys;

    public bool TryHandlePlayerDeath(IPlayerHealthController playerHealth)
    {
        if (Owner == null || Owner.AttachedItem == null || Context == null || playerHealth == null)
        {
            return false;
        }

        float baseHealth = Owner.GetParameter(ModifierParameterKeys.ReviveBaseHealthPercent, 0.25f);
        float perStack = Owner.GetParameter(ModifierParameterKeys.RevivePerStackHealthPercent, 0.1f);
        float reviveHealth = Mathf.Clamp01(baseHealth + perStack * Mathf.Max(0, Owner.AttachedItemStackSize - 1));
        float invincibilityDuration = Owner.GetParameter(ModifierParameterKeys.ReviveInvincibilityDuration, 1.5f);

        if (!Context.Inventory.RemoveItemStack(Owner.AttachedItem, 1))
        {
            return false;
        }

        playerHealth.Revive(reviveHealth, invincibilityDuration);
        return true;
    }
}
