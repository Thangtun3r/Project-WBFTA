public class MaxHealthShieldGrantLogic : ItemLogicBase, IItemEventListener
{
    private const string MaxHealthPercentKey = "ShieldGrant.MaxHealthPercent";

    protected override void OnInitialize()
    {
        RefreshGrant();
    }

    protected override void HandleStackChanged(int amountChanged)
    {
        RefreshGrant();
    }

    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (itemEvent.Type == ItemEventType.ItemEquipped || itemEvent.Type == ItemEventType.ItemRemoved)
        {
            RefreshGrant();
        }
    }

    public override void Dispose()
    {
        PlayerHealth playerHealth = GetPlayerHealth();
        if (playerHealth != null)
        {
            playerHealth.RemoveItemHealthGrant(Owner);
        }
    }

    private void RefreshGrant()
    {
        PlayerHealth playerHealth = GetPlayerHealth();
        if (playerHealth == null || Owner == null)
        {
            return;
        }

        float shieldPercent = GetPercentPerStack() * Owner.StackSize;
        playerHealth.SetItemHealthGrant(Owner, bonusMaxShieldPercentOfMaxHealth: shieldPercent);
    }

    private float GetPercentPerStack()
    {
        return Owner != null
            ? Owner.GetParameter(MaxHealthPercentKey, 0.2f)
            : 0.2f;
    }

    private PlayerHealth GetPlayerHealth()
    {
        if (Owner == null || Owner.OwnerObject == null)
        {
            return null;
        }

        return Owner.OwnerObject.GetComponent<PlayerHealth>() ??
               Owner.OwnerObject.GetComponentInParent<PlayerHealth>();
    }
}
