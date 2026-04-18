

public class CritRateLogic : ItemLogicBase, ICritRateLogic
{
    public float critRateBonus = 0.2f; 
    public float increasePerStack = 0.1f;


    public float AddCritRateBonus()
    {
        int stackCount = Owner.StackSize;
        return critRateBonus + (stackCount - 1) * increasePerStack;
    }
}