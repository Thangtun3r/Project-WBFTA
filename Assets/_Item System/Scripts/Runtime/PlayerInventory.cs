using System;
using System.Collections.Generic;
using _Scripts.Enemy;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private List<ItemRuntime> activeItems = new List<ItemRuntime>();
    public event Action<ItemRuntime> InventoryUpdated;
    public ItemSystemContext ItemContext { get; private set; }

    private bool _subscribedToGlobalEvents;

    private void Awake()
    {
        ItemContext = new ItemSystemContext(this);
    }

    private void OnEnable()
    {
        SubscribeToGlobalEvents();
    }

    private void Start()
    {
        SubscribeToGlobalEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromGlobalEvents();
    }

    public void ProcessPickup(string itemId)
    {
        var (definition, logic) = ItemDatabaseFactory.Instance.CreateItem(itemId);
        
        if (definition == null) return;

        ItemRuntime existingItem = FindItemByDefinition(definition);
        if (existingItem != null)
        {
            UpdateExistingItem(existingItem);
        }
        else
        {
            AddNewItem(definition, logic);
        }
    }

    public IReadOnlyList<ItemRuntime> GetActiveItems()
    {
        return activeItems;
    }

    public ItemRuntime FindRuntimeItem(string itemId)
    {
        if (ItemDatabaseFactory.Instance == null)
        {
            return null;
        }

        ItemDefinition definition = ItemDatabaseFactory.Instance.GetDefinition(itemId);
        return definition != null ? FindItemByDefinition(definition) : null;
    }

    private void UpdateExistingItem(ItemRuntime item)
    {
        item.AddStack(1);
        InventoryUpdated?.Invoke(item);
    }

    private void AddNewItem(ItemDefinition definition, IItemLogic logic)
    {
        ItemRuntime newItem = new ItemRuntime();
        newItem.Initialize(definition, logic, this.gameObject, ItemContext);
        activeItems.Add(newItem);
        ItemContext.PublishEvent(new ItemEvent
        {
            Type = ItemEventType.ItemEquipped,
            SourceItem = newItem,
            Owner = gameObject,
            ProcCoefficient = 1f
        });
        InventoryUpdated?.Invoke(newItem);
    }

    public void RemoveItem(string itemId)
    {
        // Use the factory to get the definition so we know what we are looking for
        var definition = ItemDatabaseFactory.Instance.GetDefinition(itemId);
        if (definition == null) return;

        ItemRuntime existingItem = FindItemByDefinition(definition);
        
        if (existingItem != null)
        {
            existingItem.DecreaseStack(1);

            // If we have 0 left, completely remove it from the inventory
            if (existingItem.StackSize <= 0)
            {
                existingItem.Remove(); // This calls Dispose() on the logic script!
                activeItems.Remove(existingItem);
                ItemContext.PublishEvent(new ItemEvent
                {
                    Type = ItemEventType.ItemRemoved,
                    SourceItem = existingItem,
                    Owner = gameObject,
                    ProcCoefficient = 1f
                });
            }

            // Tell the UI that the inventory changed
            InventoryUpdated?.Invoke(existingItem);
        }
    }

    public ModifierRuntime AttachModifierToItem(ItemRuntime item, string modifierId)
    {
        if (item == null || ModifierDatabaseFactory.Instance == null)
        {
            return null;
        }

        var (definition, logic) = ModifierDatabaseFactory.Instance.CreateModifier(modifierId);
        ModifierRuntime modifier = item.AttachModifier(definition, logic);
        if (modifier != null)
        {
            InventoryUpdated?.Invoke(item);
        }

        return modifier;
    }

    public ModifierRuntime AttachModifierToItem(string itemId, string modifierId)
    {
        return AttachModifierToItem(FindRuntimeItem(itemId), modifierId);
    }

    public bool RemoveModifierFromItem(ItemRuntime item, string modifierId)
    {
        if (item == null || ModifierDatabaseFactory.Instance == null)
        {
            return false;
        }

        ModifierDefinition definition = ModifierDatabaseFactory.Instance.GetDefinition(modifierId);
        return RemoveModifierFromItem(item, definition);
    }

    public bool RemoveModifierFromItem(ItemRuntime item, ModifierDefinition definition)
    {
        if (item == null || definition == null)
        {
            return false;
        }

        bool removed = item.RemoveModifier(definition);
        if (removed)
        {
            InventoryUpdated?.Invoke(item);
        }

        return removed;
    }

    public bool RemoveModifierFromItem(string itemId, string modifierId)
    {
        return RemoveModifierFromItem(FindRuntimeItem(itemId), modifierId);
    }

    public bool RemoveItemStack(ItemRuntime item, int amount = 1)
    {
        if (item == null || amount <= 0 || !activeItems.Contains(item))
        {
            return false;
        }

        item.DecreaseStack(amount);
        if (item.StackSize <= 0)
        {
            item.Remove();
            activeItems.Remove(item);
            ItemContext.PublishEvent(new ItemEvent
            {
                Type = ItemEventType.ItemRemoved,
                SourceItem = item,
                Owner = gameObject,
                ProcCoefficient = 1f
            });
        }

        InventoryUpdated?.Invoke(item);
        return true;
    }

    private ItemRuntime FindItemByDefinition(ItemDefinition definition)
    {
        return activeItems.Find(item => item.Definition == definition);
    }

    private void SubscribeToGlobalEvents()
    {
        if (_subscribedToGlobalEvents || GlobalEventManager.Instance == null)
        {
            return;
        }

        GlobalEventManager.Instance.HandleOnHit += HandleGlobalHit;
        GlobalEventManager.Instance.OnEnemyKilledWithStats += HandleEnemyKilled;
        _subscribedToGlobalEvents = true;
    }

    private void UnsubscribeFromGlobalEvents()
    {
        if (!_subscribedToGlobalEvents || GlobalEventManager.Instance == null)
        {
            return;
        }

        GlobalEventManager.Instance.HandleOnHit -= HandleGlobalHit;
        GlobalEventManager.Instance.OnEnemyKilledWithStats -= HandleEnemyKilled;
        _subscribedToGlobalEvents = false;
    }

    private void HandleGlobalHit(GameObject attacker, IDamagable target, float damage, bool isCrit, float procCoefficient)
    {
        ItemContext.PublishEvent(new ItemEvent
        {
            Type = ItemEventType.HitEnemy,
            Owner = gameObject,
            Attacker = attacker,
            Target = target,
            Damage = damage,
            ProcCoefficient = Mathf.Max(0f, procCoefficient),
            IsCrit = isCrit
        });
    }

    private void HandleEnemyKilled(BaseEnemy enemy, float damage, bool isCrit)
    {
        ItemContext.PublishEvent(new ItemEvent
        {
            Type = ItemEventType.EnemyKilled,
            Owner = gameObject,
            Enemy = enemy,
            Damage = damage,
            ProcCoefficient = 1f,
            IsCrit = isCrit
        });
    }
}
