using System.Collections.Generic;
using UnityEngine;

public class InventoryVisualManager : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform inventoryUIParent;

    private PlayerInventory playerInventory;
    private readonly Dictionary<ItemDefinition, ItemRuntimeVisual> visualsByDefinition =
        new Dictionary<ItemDefinition, ItemRuntimeVisual>();

    private void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();

        if (inventoryUIParent == null)
        {
            inventoryUIParent = transform;
        }
    }

    void OnEnable()
    {
        PlayerInventory.OnInventoryUpdated += UpdateVisual;
    }
    void OnDisable()
    {
        PlayerInventory.OnInventoryUpdated -= UpdateVisual;
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
