using UnityEngine;

public abstract class AttachedItemHealthStatMultiplierLogicBase : ItemModifier, IItemStatProvider, IItemParameterProvider
{
    public void ModifyItemStat(ref ItemStatQuery query)
    {
        if (Owner == null || Owner.AttachedItem == null || query.Item != Owner.AttachedItem)
        {
            return;
        }

        query.Multiplier *= EvaluateMultiplier(GetHealthRatio());
    }

    public void ModifyItemParameter(ref ItemParameterQuery query)
    {
        if (Owner == null || Owner.AttachedItem == null || query.Item != Owner.AttachedItem)
        {
            return;
        }

        query.Multiplier *= EvaluateMultiplier(GetHealthRatio());
    }

    protected abstract float EvaluateMultiplier(float healthRatio);

    private float GetHealthRatio()
    {
        GameObject ownerObject = Owner?.AttachedItem?.OwnerObject;
        if (ownerObject == null)
        {
            return 1f;
        }

        PlayerHealth playerHealth = ownerObject.GetComponent<PlayerHealth>() ?? ownerObject.GetComponentInParent<PlayerHealth>();
        return playerHealth != null ? playerHealth.NormalizedHealth : 1f;
    }
}
