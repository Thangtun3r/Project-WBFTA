using UnityEngine;


[CreateAssetMenu(fileName = "New Item", menuName = "Item System/Item Definition")]
public class ItemDefinition: ScriptableObject
{
    public string itemName; 
    public Sprite icon;
    public string description;
    
}