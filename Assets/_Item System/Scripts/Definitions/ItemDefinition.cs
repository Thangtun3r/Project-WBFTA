using UnityEngine;
using System.Collections.Generic;
    public enum ItemType
    {
        Offensive,
        Defensive,
        Utility
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

[CreateAssetMenu(fileName = "New Item", menuName = "Item System/Item Definition")]
public class ItemDefinition: ScriptableObject
{

    public string itemId;
    public string itemName; 
    public ItemType itemType;
    public ItemRarity itemRarity;
    public Sprite icon;
    public bool isModifiable;
    public string description;
    public string logicClassName;

    public GameObject effectPrefab;
    public List<ItemStatEntry> itemStats = new List<ItemStatEntry>();
    public List<PlayerStatEntry> playerStats = new List<PlayerStatEntry>();
    public List<ItemParameterEntry> parameters = new List<ItemParameterEntry>();
}
