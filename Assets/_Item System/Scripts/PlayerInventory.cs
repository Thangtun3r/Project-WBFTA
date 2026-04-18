using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private List<ItemRuntime> activeItems = new List<ItemRuntime>();
    public event Action<ItemRuntime> InventoryUpdated;

    public void ProcessPickup(string itemId)
    {
        var (definition, logic) = ItemDatabaseFactory.Instance.CreateItem(itemId);
        
        if (definition == null || logic == null) return;

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
    private void UpdateExistingItem(ItemRuntime item)
    {
        item.AddStack(1);
        InventoryUpdated?.Invoke(item);
    }

    private void AddNewItem(ItemDefinition definition, IItemLogic logic)
    {
        ItemRuntime newItem = new ItemRuntime();
        newItem.Initialize(definition, logic);
        activeItems.Add(newItem);
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
            }

            // Tell the UI that the inventory changed
            InventoryUpdated?.Invoke(existingItem);
        }
    }

    private ItemRuntime FindItemByDefinition(ItemDefinition definition)
    {
        return activeItems.Find(item => item.Definition == definition);
    }
}