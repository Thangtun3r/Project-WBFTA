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
        HandleStackChanged(amountChanged);
    }

    protected abstract void OnInitialize();
    // Force every item logic to explicitly decide how stack changes should be handled.
    protected abstract void HandleStackChanged(int amountChanged);
    public abstract void Dispose();
}
