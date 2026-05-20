using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemStatType
{
    ProcChance,
    DamageMultiplier,
    Radius,
    BounceCount,
    Cooldown,
    Duration,
    TriggerCount
}

public enum PlayerStatType
{
    AttackDamage,
    CritChance,
    CritDamage,
    MoveSpeedMultiplier,
    MaxHealth
}

public enum ItemEventType
{
    HitEnemy,
    EnemyKilled,
    PlayerDamaged,
    ItemTriggered,
    ItemEquipped,
    ItemRemoved
}

[Serializable]
public struct ItemStatEntry
{
    public ItemStatType statType;
    public float baseValue;
    public float perStackValue;

    public float GetValue(int stackSize)
    {
        return baseValue + Mathf.Max(0, stackSize - 1) * perStackValue;
    }
}

[Serializable]
public struct PlayerStatEntry
{
    public PlayerStatType statType;
    public float baseValue;
    public float perStackValue;

    public float GetValue(int stackSize)
    {
        return baseValue + Mathf.Max(0, stackSize - 1) * perStackValue;
    }
}

[Serializable]
public struct ItemParameterEntry
{
    public string key;
    public float baseValue;
    public float perStackValue;

    public float GetValue(int stackSize)
    {
        return baseValue + Mathf.Max(0, stackSize - 1) * perStackValue;
    }
}

[Serializable]
public struct ItemStatModifierEntry
{
    public ItemStatType statType;
    public float flatBonus;
    public float multiplierBonus;
}

[Serializable]
public struct PlayerStatModifierEntry
{
    public PlayerStatType statType;
    public float flatBonus;
    public float multiplierBonus;
}

[Serializable]
public struct ItemParameterModifierEntry
{
    public string key;
    public float flatBonus;
    public float multiplierBonus;
}

public struct ItemStatQuery
{
    public ItemRuntime Item;
    public ItemStatType StatType;
    public float BaseValue;
    public float FlatBonus;
    public float Multiplier;

    public float FinalValue => (BaseValue + FlatBonus) * Multiplier;
}

public struct PlayerStatQuery
{
    public PlayerInventory Inventory;
    public PlayerStatType StatType;
    public float BaseValue;
    public float FlatBonus;
    public float Multiplier;

    public float FinalValue => (BaseValue + FlatBonus) * Multiplier;
}

public struct ItemParameterQuery
{
    public ItemRuntime Item;
    public string Key;
    public float BaseValue;
    public float FlatBonus;
    public float Multiplier;

    public float FinalValue => (BaseValue + FlatBonus) * Multiplier;
}

public struct ItemTriggerContext
{
    public ItemRuntime SourceItem;
    public GameObject Owner;
    public IDamagable Target;
    public Vector3 Origin;
    public Vector3 Destination;
    public float Damage;
    public bool IsCrit;
}

public struct ItemEvent
{
    public ItemEventType Type;
    public ItemRuntime SourceItem;
    public GameObject Owner;
    public GameObject Attacker;
    public IDamagable Target;
    public _Scripts.Enemy.BaseEnemy Enemy;
    public float Damage;
    public bool IsCrit;
}

public static class ItemStatUtility
{
    public static bool TryGetItemStat(IReadOnlyList<ItemStatEntry> entries, ItemStatType statType, int stackSize, out float value)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].statType == statType)
                {
                    value = entries[i].GetValue(stackSize);
                    return true;
                }
            }
        }

        value = 0f;
        return false;
    }

    public static bool TryGetPlayerStat(IReadOnlyList<PlayerStatEntry> entries, PlayerStatType statType, int stackSize, out float value)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].statType == statType)
                {
                    value = entries[i].GetValue(stackSize);
                    return true;
                }
            }
        }

        value = 0f;
        return false;
    }

    public static bool TryGetParameter(IReadOnlyList<ItemParameterEntry> entries, string key, int stackSize, out float value)
    {
        if (!string.IsNullOrWhiteSpace(key) && entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                {
                    value = entries[i].GetValue(stackSize);
                    return true;
                }
            }
        }

        value = 0f;
        return false;
    }
}
