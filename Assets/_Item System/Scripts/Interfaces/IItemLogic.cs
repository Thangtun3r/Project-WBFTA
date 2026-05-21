public interface IItemLogic
{
    void Initialize(ItemRuntime owner, ItemSystemContext context);
    void OnStackChanged(int amountChanged);
    void Dispose();
}

public interface IItemEventListener
{
    void OnItemEvent(ItemEvent itemEvent);
}

public interface IItemStatProvider
{
    void ModifyItemStat(ref ItemStatQuery query);
}

public interface IPlayerStatProvider
{
    void ModifyPlayerStat(ref PlayerStatQuery query);
}

public interface IItemParameterProvider
{
    void ModifyItemParameter(ref ItemParameterQuery query);
}

public interface ITriggerableItem
{
    bool CanTrigger(ItemTriggerContext context);
    void Trigger(ItemTriggerContext context);
}

public interface IItemTriggerPreprocessor
{
    void BeforeItemTrigger(ref ItemTriggerContext context);
}

public interface IItemDropWeightProvider
{
    void ModifyItemDropWeight(ref ItemDropWeightQuery query);
}

public interface IPlayerDeathHandler
{
    bool TryHandlePlayerDeath(PlayerHealth playerHealth);
}

public interface ICritRateLogic
{
    // Keeping interfaces in case needed by UI/Tooltip, but no longer used in stat aggregation
    float AddCritRateBonus();
}

public interface ICritDamageLogic
{
    float AddCritDamageBonus();
}
