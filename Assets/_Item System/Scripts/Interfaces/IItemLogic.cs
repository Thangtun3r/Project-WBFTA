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
    bool TryHandlePlayerDeath(IPlayerHealthController playerHealth);
}

public interface IPlayerCombatStatSource
{
    float GetCalculatedAttackDamage();
    bool WasLastAttackCrit();
}

public interface IPlayerHealthController
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    float NormalizedHealth { get; }
    void Heal(float rawAmount = 0f, float percentageAmount = 0f);
    void Revive(float normalizedHealth, float invincibilityDuration = 0f);
}

public interface IPlayerItemHealthGrantReceiver
{
    void SetItemHealthGrant(
        ItemRuntime sourceItem,
        float bonusMaxHealth = 0f,
        float bonusMaxShield = 0f,
        float bonusMaxShieldPercentOfMaxHealth = 0f,
        float bonusMaxOverheal = 0f,
        float reservedHealthPercent = 0f);

    void RemoveItemHealthGrant(ItemRuntime sourceItem);
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
