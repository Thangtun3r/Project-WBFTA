using UnityEngine;

public interface IItemModifier
{
    void Initialize(ModifierRuntime owner, ItemSystemContext context);
    void Dispose();
}

public interface IModifierLogic : IItemModifier
{
}
