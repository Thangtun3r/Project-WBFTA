public abstract class ItemLogicBase : IItemLogic
{
    // The "Eyes" of the logic: protected so children can see it
    protected ItemRuntime Owner { get; private set; }

    public virtual void Initialize(ItemRuntime owner)
    {
        Owner = owner;
        
        // This is where you'd call a helper to subscribe to events
        OnInitialize();
    }

    public virtual void Dispose()
    {
        // This is where you'd call a helper to unsubscribe
        OnDispose();
        
        // Clear the reference to help the Garbage Collector
        Owner = null;
    }

    // Helper methods for children to use instead of overriding Initialize/Dispose
    protected virtual void OnInitialize() { }
    protected virtual void OnDispose() { }
}