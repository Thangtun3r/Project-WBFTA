using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Modifier", menuName = "Item System/Modifier Definition")]
public class ModifierDefinition : ScriptableObject
{
    public string modifierId;
    public string modifierName;
    public string description;
    public ItemRarity rarity;
    public float procCoefficient = 1f;

    public List<ItemStatModifierEntry> itemStatModifiers = new List<ItemStatModifierEntry>();
    public List<PlayerStatModifierEntry> playerStatModifiers = new List<PlayerStatModifierEntry>();
    public List<ItemParameterModifierEntry> parameterModifiers = new List<ItemParameterModifierEntry>();
}
