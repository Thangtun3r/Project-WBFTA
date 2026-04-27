public abstract class ItemLogicBase : IItemLogic
{
    // This provides the 'Owner' context to every child class
    protected ItemRuntime Owner; 

    public void Initialize(ItemRuntime runtime)
    {
        // Store the reference passed from the ItemRuntime
        Owner = runtime; 
        
        OnInitialize();
    }

    public virtual void OnStackChanged(int amountChanged)
    {
        // Override in specific item logic scripts if stacking affects stats
    }

    protected abstract void OnInitialize();
    public abstract void Dispose();
}