using System.Collections.Generic;

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
                IsCrit = itemEvent.IsCrit
            });
        }
    }
}
