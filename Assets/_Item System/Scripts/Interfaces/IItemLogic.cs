public interface IItemLogic
{
    void Initialize(ItemRuntime owner);
    void Dispose();
}


public interface ICritRateLogic
{
    float AddCritRateBonus();
}

public interface ICritDamageLogic
{
    float AddCritDamageBonus();
}
