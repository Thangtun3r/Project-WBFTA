using UnityEngine;

public class TriggerAttachedItemOnPlayerDamagedLogic : ItemModifier, IItemEventListener
{
    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (Owner == null || Owner.AttachedItem == null || itemEvent.Type != ItemEventType.PlayerDamaged)
        {
            return;
        }

        GameObject ownerObject = Owner.AttachedItem.OwnerObject;
        if (ownerObject == null || itemEvent.Owner != ownerObject)
        {
            return;
        }

        float triggerChance = Owner.Definition != null ? Owner.Definition.procCoefficient : 0f;
        if (triggerChance <= 0f || Random.value > triggerChance)
        {
            return;
        }

        Owner.AttachedItem.RequestTrigger(new ItemTriggerContext
        {
            SourceItem = Owner.AttachedItem,
            Owner = ownerObject,
            Origin = ownerObject.transform.position,
            Damage = itemEvent.Damage,
            ProcCoefficient = 1f
        });
    }
}
