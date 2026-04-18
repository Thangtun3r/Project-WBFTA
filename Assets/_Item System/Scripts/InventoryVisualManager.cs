using System.Collections.Generic;
using UnityEngine;

public class InventoryVisualManager : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform inventoryUIParent;

    private PlayerInventory playerInventory;
    private readonly Dictionary<ItemDefinition, ItemRuntimeVisual> visualsByDefinition =
        new Dictionary<ItemDefinition, ItemRuntimeVisual>();

    private void Awake()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();

        if (playerInventory == null)
        {
            Debug.LogWarning("InventoryVisualManager: No PlayerInventory found in scene.");
        }

        if (inventoryUIParent == null)
        {
            inventoryUIParent = transform;
        }
    }

    private void OnEnable()
    {
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }

        if (playerInventory != null)
        {
            playerInventory.InventoryUpdated += UpdateVisual;
        }
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryUpdated -= UpdateVisual;
        }
    }

    private void UpdateVisual(ItemRuntime itemRuntime)
    {
        if (itemRuntime == null)
        {
            return;
        }

        ItemDefinition definition = itemRuntime.Definition;
        if (definition == null)
        {
            return;
        }

        if (itemRuntime.StackSize <= 0)
        {
            if (visualsByDefinition.TryGetValue(definition, out ItemRuntimeVisual existingVisual))
            {
                Destroy(existingVisual.gameObject);
                visualsByDefinition.Remove(definition);
            }
            return;
        }

        if (visualsByDefinition.TryGetValue(definition, out ItemRuntimeVisual visual))
        {
            visual.SetData(itemRuntime);
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogWarning("InventoryVisualManager: slotPrefab is not assigned.");
            return;
        }

        GameObject slotObject = Instantiate(slotPrefab, inventoryUIParent);
        ItemRuntimeVisual slotVisual = slotObject.GetComponent<ItemRuntimeVisual>();
        if (slotVisual == null)
        {
            Debug.LogWarning("InventoryVisualManager: slotPrefab is missing ItemRuntimeVisual.");
            return;
        }

        slotVisual.SetData(itemRuntime);
        visualsByDefinition[definition] = slotVisual;
    }

}
