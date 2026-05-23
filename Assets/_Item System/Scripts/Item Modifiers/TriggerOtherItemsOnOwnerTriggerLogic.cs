using System.Collections.Generic;
using UnityEngine;

public class TriggerOtherItemsOnOwnerTriggerLogic : ItemModifier, IItemEventListener
{
    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (Owner == null || Context == null || itemEvent.Type != ItemEventType.ItemTriggered)
        {
            return;
        }

        if (itemEvent.SourceItem != Owner.AttachedItem)
        {
            return;
        }

        IReadOnlyList<ItemRuntime> items = Context.Inventory.GetActiveItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemRuntime candidate = items[i];
            if (candidate == null || candidate == Owner.AttachedItem)
            {
                continue;
            }

            candidate.RequestTrigger(new ItemTriggerContext
            {
                SourceItem = Owner.AttachedItem,
                Owner = itemEvent.Owner,
                Target = itemEvent.Target,
                Damage = itemEvent.Damage,
                ProcCoefficient = Mathf.Max(0f, itemEvent.ProcCoefficient),
                IsCrit = itemEvent.IsCrit
            });
        }
    }
}
