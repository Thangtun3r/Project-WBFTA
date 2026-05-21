using System.Collections.Generic;

public interface IItemModifier
{
    void Initialize(ModifierRuntime owner, ItemSystemContext context);
    void Dispose();
}

public interface IModifierLogic : IItemModifier
{
}

public interface IModifierParameterRequirements
{
    IReadOnlyList<string> RequiredParameterKeys { get; }
}
