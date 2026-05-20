public class ItemModifier : IModifierLogic
{
    protected ModifierRuntime Owner;
    protected ItemSystemContext Context;

    public virtual void Initialize(ModifierRuntime owner, ItemSystemContext context)
    {
        Owner = owner;
        Context = context;
    }

    public virtual void Dispose()
    {
    }
}
