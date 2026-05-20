using System.Collections.Generic;
using UnityEngine;

public class ItemSystemContext
{
    private readonly PlayerInventory _inventory;
    private int _triggerDepth;

    public PlayerInventory Inventory => _inventory;
    public GameObject OwnerObject => _inventory != null ? _inventory.gameObject : null;
    public int MaxTriggerDepth { get; set; } = 8;

    public ItemSystemContext(PlayerInventory inventory)
    {
        _inventory = inventory;
    }

    public void PublishEvent(ItemEvent itemEvent)
    {
        IReadOnlyList<ItemRuntime> items = _inventory.GetActiveItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemRuntime item = items[i];
            if (item == null)
            {
                continue;
            }

            if (item.Logic is IItemEventListener listener)
            {
                listener.OnItemEvent(itemEvent);
            }

            IReadOnlyList<ModifierRuntime> modifiers = item.Modifiers;
            for (int m = 0; m < modifiers.Count; m++)
            {
                modifiers[m]?.OnItemEvent(itemEvent);
            }
        }
    }

    public float CalculateItemStat(ItemRuntime item, ItemStatType statType, float fallbackValue)
    {
        float baseValue = fallbackValue;
        if (item != null && item.Definition != null &&
            ItemStatUtility.TryGetItemStat(item.Definition.itemStats, statType, item.StackSize, out float definitionValue))
        {
            baseValue = definitionValue;
        }

        ItemStatQuery query = new ItemStatQuery
        {
            Item = item,
            StatType = statType,
            BaseValue = baseValue,
            FlatBonus = 0f,
            Multiplier = 1f
        };

        IReadOnlyList<ItemRuntime> items = _inventory.GetActiveItems();
        for (int i = 0; i < items.Count; i++)
        {
            ApplyItemStatProviders(items[i], ref query);
        }

        return query.FinalValue;
    }

    public float CalculateParameter(ItemRuntime item, string key, float fallbackValue)
    {
        float baseValue = fallbackValue;
        if (item != null && item.Definition != null &&
            ItemStatUtility.TryGetParameter(item.Definition.parameters, key, item.StackSize, out float definitionValue))
        {
            baseValue = definitionValue;
        }

        ItemParameterQuery query = new ItemParameterQuery
        {
            Item = item,
            Key = key,
            BaseValue = baseValue,
            FlatBonus = 0f,
            Multiplier = 1f
        };

        IReadOnlyList<ItemRuntime> items = _inventory.GetActiveItems();
        for (int i = 0; i < items.Count; i++)
        {
            ApplyParameterProviders(items[i], ref query);
        }

        return query.FinalValue;
    }

    public float CalculatePlayerStat(PlayerStatType statType, float baseValue)
    {
        PlayerStatQuery query = new PlayerStatQuery
        {
            Inventory = _inventory,
            StatType = statType,
            BaseValue = baseValue,
            FlatBonus = 0f,
            Multiplier = 1f
        };

        IReadOnlyList<ItemRuntime> items = _inventory.GetActiveItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemRuntime item = items[i];
            if (item == null)
            {
                continue;
            }

            if (item.Definition != null &&
                ItemStatUtility.TryGetPlayerStat(item.Definition.playerStats, statType, item.StackSize, out float itemValue))
            {
                query.FlatBonus += itemValue;
            }

            ApplyPlayerStatProviders(item, ref query);
        }

        return query.FinalValue;
    }

    public bool RequestTrigger(ItemRuntime item, ItemTriggerContext triggerContext)
    {
        if (item == null || item.Logic is not ITriggerableItem triggerable)
        {
            return false;
        }

        if (_triggerDepth >= MaxTriggerDepth)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"ItemSystemContext: Max trigger depth {MaxTriggerDepth} reached. Trigger ignored.");
#endif
            return false;
        }

        if (!triggerable.CanTrigger(triggerContext))
        {
            return false;
        }

        _triggerDepth++;
        try
        {
            triggerable.Trigger(triggerContext);
            PublishEvent(new ItemEvent
            {
                Type = ItemEventType.ItemTriggered,
                SourceItem = item,
                Owner = item.OwnerObject,
                Target = triggerContext.Target,
                Damage = triggerContext.Damage,
                IsCrit = triggerContext.IsCrit
            });
        }
        finally
        {
            _triggerDepth--;
        }

        return true;
    }

    private static void ApplyItemStatProviders(ItemRuntime item, ref ItemStatQuery query)
    {
        if (item == null)
        {
            return;
        }

        if (item.Logic is IItemStatProvider provider)
        {
            provider.ModifyItemStat(ref query);
        }

        IReadOnlyList<ModifierRuntime> modifiers = item.Modifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i]?.ModifyItemStat(ref query);
        }
    }

    private static void ApplyPlayerStatProviders(ItemRuntime item, ref PlayerStatQuery query)
    {
        if (item == null)
        {
            return;
        }

        if (item.Logic is IPlayerStatProvider provider)
        {
            provider.ModifyPlayerStat(ref query);
        }

        IReadOnlyList<ModifierRuntime> modifiers = item.Modifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i]?.ModifyPlayerStat(ref query);
        }
    }

    private static void ApplyParameterProviders(ItemRuntime item, ref ItemParameterQuery query)
    {
        if (item == null)
        {
            return;
        }

        if (item.Logic is IItemParameterProvider provider)
        {
            provider.ModifyItemParameter(ref query);
        }

        IReadOnlyList<ModifierRuntime> modifiers = item.Modifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i]?.ModifyItemParameter(ref query);
        }
    }
}
