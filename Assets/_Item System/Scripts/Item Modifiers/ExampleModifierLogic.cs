// Reference template only.
// Do not add this exact class to ModifierDatabaseFactory as gameplay logic.
//
// Use this file as a pattern when creating a behavior modifier:
// - ItemModifier gives access to Owner, which is the ModifierRuntime.
// - Owner.AttachedItem is the item this modifier is attached to.
// - Owner.AttachedItemStackSize is the attached item's stack count. Modifiers do not stack by themselves.
// - Optional interfaces decide which channels this modifier participates in.
public class ExampleModifierLogic : ItemModifier, IItemEventListener, IItemStatProvider
{
    public void OnItemEvent(ItemEvent itemEvent)
    {
        // Owner can be null before Initialize, so guard custom logic.
        if (Owner == null || Owner.AttachedItem == null)
        {
            return;
        }

        // This modifier only cares when the item it is attached to triggers.
        // This is how modifiers "listen to" their item without knowing the concrete item logic class.
        if (itemEvent.Type != ItemEventType.ItemTriggered || itemEvent.SourceItem != Owner.AttachedItem)
        {
            return;
        }

        // React to the attached item triggering here. Example: request another item trigger.
    }

    public void ModifyItemStat(ref ItemStatQuery query)
    {
        // Stat query methods are how modifiers alter final item stats.
        // Do not mutate fields on item logic classes directly.
        if (Owner == null || query.Item != Owner.AttachedItem)
        {
            return;
        }

        // This example adds +5% proc chance per stack of the attached item.
        // Because modifiers do not stack, scaling comes from Owner.AttachedItemStackSize.
        if (query.StatType == ItemStatType.ProcChance)
        {
            query.FlatBonus += 0.05f * Owner.AttachedItemStackSize;
        }
    }
}
