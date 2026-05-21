public class IncreaseAttachedItemDropChanceLogic : ItemModifier, IItemDropWeightProvider
{
    public void ModifyItemDropWeight(ref ItemDropWeightQuery query)
    {
        if (Owner == null || Owner.AttachedItem == null || Owner.AttachedItem.Definition != query.CandidateDefinition)
        {
            return;
        }

        float multiplierBonus = Owner.Definition != null ? Owner.Definition.procCoefficient : 0.1f;
        query.Multiplier *= 1f + multiplierBonus * Owner.AttachedItemStackSize;
    }
}
