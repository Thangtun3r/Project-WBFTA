using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public int CurrentMoney { get; private set; }

    public static event Action<int> OnMoneyChanged;

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
        CurrentMoney = 0;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
        Debug.Log($"Added {amount} money! Current money: {CurrentMoney}");
    }

    public void RemoveMoney(int amount)
    {
        if (amount <= 0)
            return;

        CurrentMoney = Mathf.Max(0, CurrentMoney - amount);
        OnMoneyChanged?.Invoke(CurrentMoney);
        Debug.Log($"Removed {amount} money! Current money: {CurrentMoney}");
    }

    public bool TryRemoveMoney(int amount)
    {
        if (CurrentMoney >= amount)
        {
            RemoveMoney(amount);
            return true;
        }
        return false;
    }
}
