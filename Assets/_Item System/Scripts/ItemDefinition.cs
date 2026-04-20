using UnityEngine;
    public enum ItemType
    {
        Offensive,
        Defensive,
        Utility
    }

[CreateAssetMenu(fileName = "New Item", menuName = "Item System/Item Definition")]
public class ItemDefinition: ScriptableObject
{

    public string itemName; 
    public ItemType itemType;
    public Sprite icon;
    public string description;

    public GameObject effectPrefab;
    
}