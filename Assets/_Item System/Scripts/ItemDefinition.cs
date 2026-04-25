using UnityEngine;
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

    public string itemName; 
    public ItemType itemType;
    public ItemRarity itemRarity;
    public Sprite icon;
    public string description;

    public GameObject effectPrefab;
    
}