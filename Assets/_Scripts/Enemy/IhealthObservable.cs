public interface IHealthObservable
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    // An event so the UI knows exactly when to refresh
    event System.Action<float, float> OnHealthChanged;
}