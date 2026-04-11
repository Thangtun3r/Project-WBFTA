using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public float CurrentMoney { get; private set; }

    public static event Action<float> OnMoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CurrentMoney = 0f;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public void AddMoney(float amount)
    {
        if (amount <= 0)
            return;

        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
        Debug.Log($"Added {amount} money! Current money: {CurrentMoney}");
    }

    public void RemoveMoney(float amount)
    {
        if (amount <= 0)
            return;

        CurrentMoney = Mathf.Max(0, CurrentMoney - amount);
        OnMoneyChanged?.Invoke(CurrentMoney);
        Debug.Log($"Removed {amount} money! Current money: {CurrentMoney}");
    }

    public bool TryRemoveMoney(float amount)
    {
        if (CurrentMoney >= amount)
        {
            RemoveMoney(amount);
            return true;
        }
        return false;
    }
}
