
using _Scripts;

public class SpeedBoostLogic : ItemLogicBase
{
    public float speedBonus =  0.14f; // 10%
    public float increasePerStack = 0.14f; // 10%

    public float GetBonusMultiplier()
    {
        int stackCount = Owner.StackSize;
        return speedBonus + ((stackCount - 1) * increasePerStack);
    }

    protected override void OnInitialize() 
    {
        var movement = Owner.OwnerObject.GetComponent<Movement>();
        if (movement != null)
        {
            movement.AddSpeedMultiplier(speedBonus);
        }
    }

    public override void OnStackChanged(int amountChanged)
    {
        var movement = Owner.OwnerObject.GetComponent<Movement>();
        if (movement != null)
        {
            movement.AddSpeedMultiplier(amountChanged * increasePerStack);
        }
    }

    public override void Dispose() 
    {
        var movement = Owner.OwnerObject.GetComponent<Movement>();
        if (movement != null)
        {
            movement.AddSpeedMultiplier(-GetBonusMultiplier());
        }
    }
}