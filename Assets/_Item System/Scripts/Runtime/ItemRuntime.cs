using System.Collections.Generic;

public class ItemRuntime
{
    private ItemDefinition _definition;
    private int _stackSize;
    private IItemLogic _logic;
    private List<ModifierRuntime> _modifiers = new List<ModifierRuntime>();

    public ItemDefinition Definition => _definition;
    public int StackSize => _stackSize;
    public UnityEngine.GameObject OwnerObject { get; private set; } // Reference to the player/owner
    public IReadOnlyList<ModifierRuntime> Modifiers => _modifiers;
    public IItemLogic Logic => _logic;
    public ItemSystemContext Context { get; private set; }

    public void Initialize(ItemDefinition definition, IItemLogic logic, UnityEngine.GameObject owner, ItemSystemContext context)
    {
        _definition = definition;
        OwnerObject = owner;
        _stackSize = 1;
        _logic = logic;
        Context = context;
        
        _logic?.Initialize(this, context);
    }

    public void TriggerEffect(EffectContext context)
    {
        // Safety check: if it's a passive item with no prefab, just stop
        if (_definition.effectPrefab == null) return;

        // 1. Spawn the visual prefab
        UnityEngine.GameObject go = UnityEngine.Object.Instantiate(_definition.effectPrefab);

        // 2. Pass the context to the prefab's logic
        if (go.TryGetComponent<IItemEffect>(out var effect))
        {
            effect.ApplyEffect(context);
        }
    }

    public ModifierRuntime AttachModifier(ModifierDefinition definition, IModifierLogic logic)
    {
        if (definition == null)
        {
            return null;
        }

        ModifierRuntime existingModifier = FindModifier(definition);
        if (existingModifier != null)
        {
            return existingModifier;
        }

        ModifierRuntime modifier = new ModifierRuntime();
        modifier.Initialize(definition, logic, this, Context);
        _modifiers.Add(modifier);
        return modifier;
    }

    public bool RemoveModifier(ModifierDefinition definition)
    {
        ModifierRuntime modifier = FindModifier(definition);
        if (modifier == null)
        {
            return false;
        }

        modifier.Dispose();
        _modifiers.Remove(modifier);
        return true;
    }

    public bool HasModifier(ModifierDefinition definition)
    {
        return GetModifier(definition) != null;
    }

    public ModifierRuntime GetModifier(ModifierDefinition definition)
    {
        return FindModifier(definition);
    }

    public float GetItemStat(ItemStatType statType, float fallbackValue = 0f)
    {
        return Context != null ? Context.CalculateItemStat(this, statType, fallbackValue) : fallbackValue;
    }

    public float GetParameter(string key, float fallbackValue = 0f)
    {
        return Context != null ? Context.CalculateParameter(this, key, fallbackValue) : fallbackValue;
    }

    public bool RequestTrigger(ItemTriggerContext context)
    {
        return Context != null && Context.RequestTrigger(this, context);
    }

    public void AddStack(int amount = 1)
    {
        if (amount > 0)
        {
            _stackSize += amount;
            _logic?.OnStackChanged(amount);
        }
    }
    public void DecreaseStack(int amount = 1)
    {
        int oldSize = _stackSize;
        _stackSize -= amount;
        // We don't want negative stacks!
        if (_stackSize < 0) _stackSize = 0;

        int diff = _stackSize - oldSize; // Will be negative
        if (diff != 0)
        {
            _logic?.OnStackChanged(diff);
        }
    }

    public void Remove()
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            _modifiers[i]?.Dispose();
        }

        _modifiers.Clear();
        _logic?.Dispose();
    }

    private ModifierRuntime FindModifier(ModifierDefinition definition)
    {
        return _modifiers.Find(modifier => modifier.Definition == definition);
    }
}
