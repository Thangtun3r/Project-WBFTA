

public class CritDamageLogic : ItemLogicBase, ICritDamageLogic
{
    
    public float critDamageBonus = 0.2f; 
    public float increasePerStack = 0.23f;


    public float AddCritDamageBonus()
    {
        int stackCount = Owner.StackSize;
        return critDamageBonus + ((stackCount - 1) * increasePerStack);
    }
}