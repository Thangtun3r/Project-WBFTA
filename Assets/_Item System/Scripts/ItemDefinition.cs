using UnityEngine;

public enum ItemType
{
    offensive,
    defensive,
    utility
}
[CreateAssetMenu(fileName = "New Item", menuName = "Item System/Item Definition")]
public class ItemDefinition: ScriptableObject
{
    public string itemName; 
    public ItemType itemType;
    public Sprite icon;
    public string description;
    
}