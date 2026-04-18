using System.Collections.Generic;

public class ItemRuntime
{
    private ItemDefinition _definition;
    private int _stackSize;
    private IItemLogic _logic;
    private List<IItemModifier> _modifiers = new List<IItemModifier>();

    public ItemDefinition Definition => _definition;
    public int StackSize => _stackSize;
    public List<IItemModifier> Modifiers => _modifiers;
    public IItemLogic Logic => _logic;

    public void Initialize(ItemDefinition definition, IItemLogic logic)
    {
        _definition = definition;
        _stackSize = 1;
        _logic = logic;
        
        _logic?.Initialize(this);
    }

    public void AddStack(int amount = 1)
    {
        if (amount > 0) _stackSize += amount;
    }
    public void DecreaseStack(int amount = 1)
    {
        _stackSize -= amount;
        // We don't want negative stacks!
        if (_stackSize < 0) _stackSize = 0; 
    }

    public void Remove()
    {
        _logic?.Dispose();
    }
}