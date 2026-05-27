public class ModifierRuntime : IItemStatProvider, IPlayerStatProvider, IItemParameterProvider, IItemEventListener
{
    private ModifierDefinition _definition;
    private IModifierLogic _logic;

    public ModifierDefinition Definition => _definition;
    public IModifierLogic Logic => _logic;
    public int AttachedItemStackSize => AttachedItem != null ? AttachedItem.StackSize : 1;
    public ItemRuntime AttachedItem { get; private set; }
    public ItemSystemContext Context { get; private set; }

    public void Initialize(ModifierDefinition definition, IModifierLogic logic, ItemRuntime attachedItem, ItemSystemContext context)
    {
        _definition = definition;
        _logic = logic;
        AttachedItem = attachedItem;
        Context = context;

        _logic?.Initialize(this, context);
    }

    public void Dispose()
    {
        _logic?.Dispose();
    }

    public void ModifyItemStat(ref ItemStatQuery query)
    {
        ApplyDefinitionItemStatModifiers(ref query);

        if (_logic is IItemStatProvider provider)
        {
            provider.ModifyItemStat(ref query);
        }
    }

    public void ModifyPlayerStat(ref PlayerStatQuery query)
    {
        ApplyDefinitionPlayerStatModifiers(ref query);

        if (_logic is IPlayerStatProvider provider)
        {
            provider.ModifyPlayerStat(ref query);
        }
    }

    public void ModifyItemParameter(ref ItemParameterQuery query)
    {
        ApplyDefinitionParameterModifiers(ref query);

        if (_logic is IItemParameterProvider provider)
        {
            provider.ModifyItemParameter(ref query);
        }
    }

    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (_logic is IItemEventListener listener)
        {
            listener.OnItemEvent(itemEvent);
        }
    }

    public void BeforeItemTrigger(ref ItemTriggerContext context)
    {
        if (_logic is IItemTriggerPreprocessor preprocessor)
        {
            preprocessor.BeforeItemTrigger(ref context);
        }
    }

    public void ModifyItemDropWeight(ref ItemDropWeightQuery query)
    {
        if (_logic is IItemDropWeightProvider provider)
        {
            provider.ModifyItemDropWeight(ref query);
        }
    }

    public bool TryHandlePlayerDeath(IPlayerHealthController playerHealth)
    {
        return _logic is IPlayerDeathHandler handler && handler.TryHandlePlayerDeath(playerHealth);
    }

    public float GetParameter(string key, float fallbackValue = 0f)
    {
        if (_definition == null)
        {
            return fallbackValue;
        }

        return ItemStatUtility.TryGetParameter(_definition.parameters, key, AttachedItemStackSize, out float value)
            ? value
            : fallbackValue;
    }

    private void ApplyDefinitionItemStatModifiers(ref ItemStatQuery query)
    {
        if (query.Item != AttachedItem || _definition == null || _definition.itemStatModifiers == null)
        {
            return;
        }

        for (int i = 0; i < _definition.itemStatModifiers.Count; i++)
        {
            ItemStatModifierEntry modifier = _definition.itemStatModifiers[i];
            if (modifier.statType != query.StatType)
            {
                continue;
            }

            int stackSize = AttachedItemStackSize;
            query.FlatBonus += modifier.flatBonus * stackSize;
            query.Multiplier *= 1f + modifier.multiplierBonus * stackSize;
        }
    }

    private void ApplyDefinitionPlayerStatModifiers(ref PlayerStatQuery query)
    {
        if (_definition == null || _definition.playerStatModifiers == null)
        {
            return;
        }

        for (int i = 0; i < _definition.playerStatModifiers.Count; i++)
        {
            PlayerStatModifierEntry modifier = _definition.playerStatModifiers[i];
            if (modifier.statType != query.StatType)
            {
                continue;
            }

            int stackSize = AttachedItemStackSize;
            query.FlatBonus += modifier.flatBonus * stackSize;
            query.Multiplier *= 1f + modifier.multiplierBonus * stackSize;
        }
    }

    private void ApplyDefinitionParameterModifiers(ref ItemParameterQuery query)
    {
        if (query.Item != AttachedItem || _definition == null || _definition.parameterModifiers == null)
        {
            return;
        }

        for (int i = 0; i < _definition.parameterModifiers.Count; i++)
        {
            ItemParameterModifierEntry modifier = _definition.parameterModifiers[i];
            if (modifier.key != query.Key)
            {
                continue;
            }

            int stackSize = AttachedItemStackSize;
            query.FlatBonus += modifier.flatBonus * stackSize;
            query.Multiplier *= 1f + modifier.multiplierBonus * stackSize;
        }
    }
}
