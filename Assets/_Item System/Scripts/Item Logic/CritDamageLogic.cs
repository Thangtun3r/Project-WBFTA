

public class CritDamageLogic : ItemLogicBase, ICritDamageLogic
{
    
    public float critDamageBonus = 0.2f; 
    public float increasePerStack = 0.23f;


    public float AddCritDamageBonus()
    {
        int stackCount = Owner.StackSize;
        return critDamageBonus + ((stackCount - 1) * increasePerStack);
    }

    protected override void OnInitialize() { /* Passive, no setup needed */ }
    public override void Dispose() { /* Passive, no cleanup needed */ }
}