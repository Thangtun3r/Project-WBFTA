

public class CritDamageLogic : ItemLogicBase, ICritDamageLogic
{
    public float critDamageBonus = 0.2f; 
    public float increasePerStack = 0.23f;


    public float AddCritDamageBonus()
    {
        int stackCount = Owner.StackSize;
        return critDamageBonus + ((stackCount - 1) * increasePerStack);
    }

    protected override void OnInitialize() 
    {
        var stats = Owner.OwnerObject.GetComponent<PlayerStatMachine>();
        if (stats != null)
        {
            stats.AddCritDamageModifier(critDamageBonus);
        }
    }

    public override void OnStackChanged(int amountChanged)
    {
        var stats = Owner.OwnerObject.GetComponent<PlayerStatMachine>();
        if (stats != null)
        {
            stats.AddCritDamageModifier(amountChanged * increasePerStack);
        }
    }

    public override void Dispose() 
    {
        var stats = Owner.OwnerObject.GetComponent<PlayerStatMachine>();
        if (stats != null)
        {
            stats.AddCritDamageModifier(-AddCritDamageBonus());
        }
    }
}