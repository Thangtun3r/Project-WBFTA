using UnityEngine;

public class GainGoldOnOwnerTriggerLogic : ItemModifier, IItemEventListener
{
    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (Owner == null || Owner.AttachedItem == null || itemEvent.Type != ItemEventType.ItemTriggered)
        {
            return;
        }

        if (itemEvent.SourceItem != Owner.AttachedItem || EconomyManager.Instance == null)
        {
            return;
        }

        int currentMoney = EconomyManager.Instance.CurrentMoney;
        if (currentMoney <= 0)
        {
            return;
        }

        float coefficient = Owner.Definition != null ? Owner.Definition.procCoefficient : 0f;
        if (coefficient <= 0f)
        {
            return;
        }

        int reward = Mathf.FloorToInt(currentMoney * coefficient * Owner.AttachedItemStackSize);
        if (reward > 0)
        {
            EconomyManager.Instance.AddMoney(reward);
        }
    }
}
