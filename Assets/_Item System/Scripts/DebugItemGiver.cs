using UnityEngine;
using System.Collections.Generic;

[System.Serializable] 
public struct DebugItemBinding
{
    public string itemId;
    public KeyCode addKey;
    public KeyCode removeKey; // The new key to test dropping!
}

public class DebugItemGiver : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    
    [Header("Debug Key Bindings")]
    [SerializeField] private List<DebugItemBinding> itemBindings = new List<DebugItemBinding>();

    private void Update()
    {
        if (inventory == null) return;

        foreach (var binding in itemBindings)
        {
            // Test the "UP" (Adding)
            if (Input.GetKeyDown(binding.addKey))
            {
                inventory.ProcessPickup(binding.itemId);
                Debug.Log($"[DEBUG] Added 1x {binding.itemId}");
            }

            // Test the "DOWN" (Removing)
            if (Input.GetKeyDown(binding.removeKey))
            {
                inventory.RemoveItem(binding.itemId);
                Debug.Log($"[DEBUG] Removed 1x {binding.itemId}");
            }
        }
    }
}