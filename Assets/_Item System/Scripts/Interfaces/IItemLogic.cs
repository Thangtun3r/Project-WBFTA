public interface IItemLogic
{
    void Initialize(ItemRuntime owner);
    void OnStackChanged(int amountChanged);
    void Dispose();
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
