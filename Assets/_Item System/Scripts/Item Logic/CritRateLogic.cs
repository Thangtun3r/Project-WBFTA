public class CritRateLogic : ItemLogicBase, ICritRateLogic
{
    public float critRateBonus = 0.2f; 
    public float increasePerStack = 0.1f;

    public float AddCritRateBonus()
    {
        int stackCount = Owner.StackSize;
        return critRateBonus + (stackCount - 1) * increasePerStack;
    }

    protected override void OnInitialize() 
    {
        var stats = Owner.OwnerObject.GetComponent<PlayerStatMachine>();
        if (stats != null)
        {
            stats.AddCritRateModifier(critRateBonus);
        }
    }

    public override void OnStackChanged(int amountChanged)
    {
        var stats = Owner.OwnerObject.GetComponent<PlayerStatMachine>();
        if (stats != null)
        {
            // amountChanged preserves the sign (+1 or -1 typically)
            stats.AddCritRateModifier(amountChanged * increasePerStack);
        }
    }

    public override void Dispose() 
    {
        var stats = Owner.OwnerObject.GetComponent<PlayerStatMachine>();
        if (stats != null)
        {
            stats.AddCritRateModifier(-AddCritRateBonus());
        }
    }
}