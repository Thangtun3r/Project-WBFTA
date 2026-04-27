public class MaxHealthIncreaseLogic : ItemLogicBase
{
    public float healthBonus = 25f;
    public float increasePerStack = 25f;

    public float GetTotalBonus()
    {
        int stackCount = Owner.StackSize;
        return healthBonus + ((stackCount - 1) * increasePerStack);
    }

    protected override void OnInitialize()
    {
        var health = Owner.OwnerObject.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Increase the cap
            health.AddMaxHealthModifier(healthBonus);
            
            // Heal them for the new max health so their HP bar doesn't look emptier
            health.Heal(rawAmount: healthBonus); 
        }
    }

    public override void OnStackChanged(int amountChanged)
    {
        var health = Owner.OwnerObject.GetComponent<PlayerHealth>();
        if (health != null)
        {
            float amountToChange = amountChanged * increasePerStack;
            health.AddMaxHealthModifier(amountToChange);
            
            // If they gained max health (not dropping an item), heal them
            if (amountToChange > 0)
            {
                health.Heal(rawAmount: amountToChange);
            }
        }
    }

    public override void Dispose()
    {
        var health = Owner.OwnerObject.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Remove the total bonus from their max health cap when disposed
            health.AddMaxHealthModifier(-GetTotalBonus());
        }
    }
}
