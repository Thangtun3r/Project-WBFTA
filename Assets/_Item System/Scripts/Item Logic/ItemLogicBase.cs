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

    protected abstract void OnInitialize();
    public abstract void Dispose();
}