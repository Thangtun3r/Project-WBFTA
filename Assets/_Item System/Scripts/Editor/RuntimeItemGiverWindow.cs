using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RuntimeItemGiverWindow : EditorWindow
{
    private PlayerInventory _targetInventory;
    private int _quantity = 1;
    private Vector2 _scrollPos;

    private struct ItemData
    {
        public string id;
        public string name;
        public Texture2D icon;
        public ItemDefinition definition;
    }

    private List<ItemData> _cachedItems = new List<ItemData>();

    [MenuItem("Window/Item System/Runtime Item Giver")]
    public static void ShowWindow()
    {
        GetWindow<RuntimeItemGiverWindow>("Give Item");
    }

    private void OnGUI()
    {
        GUILayout.Label("Runtime Item Giver", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("This tool only works while the game is playing.", MessageType.Warning);
            return;
        }

        if (_targetInventory == null)
        {
            _targetInventory = FindObjectOfType<PlayerInventory>();
        }

        _targetInventory = (PlayerInventory)EditorGUILayout.ObjectField("Target Inventory", _targetInventory, typeof(PlayerInventory), true);

        ItemDatabaseFactory factory = ItemDatabaseFactory.Instance;
        if (factory == null)
        {
            EditorGUILayout.HelpBox("ItemDatabaseFactory isn't initialized or in the scene yet.", MessageType.Warning);
            return;
        }

        RefreshItemIds(factory);

        if (_cachedItems.Count == 0)
        {
            EditorGUILayout.HelpBox("No items found in ItemDatabaseFactory.", MessageType.Info);
            return;
        }

        _quantity = EditorGUILayout.IntSlider("Quantity to Give", _quantity, 1, 100);

        EditorGUILayout.Space();
        GUILayout.Label("Item Database Grid (Click to give)", EditorStyles.boldLabel);

        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        float windowWidth = EditorGUIUtility.currentViewWidth - 25f; // offset for scrollbar
        float buttonSize = 80f;
        int columns = Mathf.Max(1, Mathf.FloorToInt(windowWidth / (buttonSize + 10f)));

        int index = 0;
        while (index < _cachedItems.Count)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // centers the grid somewhat
            
            for (int c = 0; c < columns; c++)
            {
                if (index < _cachedItems.Count)
                {
                    var item = _cachedItems[index];

                    GUILayout.BeginVertical(GUILayout.Width(buttonSize));
                    GUIContent content = (item.icon != null)
                        ? new GUIContent(item.icon, $"ID: {item.id}\nClick to give {_quantity}x")
                        : new GUIContent(item.id, $"ID: {item.id}\nClick to give {_quantity}x");

                    if (GUILayout.Button(content, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    {
                        if (_targetInventory != null)
                        {
                            for (int q = 0; q < _quantity; q++)
                            {
                                _targetInventory.ProcessPickup(item.id);
                            }
                            Debug.Log($"Gave {_quantity} of {item.id} to {_targetInventory.name}");
                        }
                        else
                        {
                            Debug.LogWarning("Target Inventory is not assigned!");
                        }
                    }

                    string displayName = !string.IsNullOrEmpty(item.name) ? item.name : item.id;
                    if (displayName.Length > 12) displayName = displayName.Substring(0, 10) + "..";

                    GUILayout.Label(displayName, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(buttonSize));
                    GUILayout.EndVertical();

                    index++;
                }
                else
                {
                    // Empty space filler for layout
                    GUILayout.Space(buttonSize);
                }
                GUILayout.Space(5); // spacing between columns
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10); // spacing between rows
        }

        GUILayout.EndScrollView();
    }

    private void RefreshItemIds(ItemDatabaseFactory factory)
    {
        SerializedObject so = new SerializedObject(factory);
        SerializedProperty itemEntriesProp = so.FindProperty("itemEntries");

        _cachedItems.Clear();

        if (itemEntriesProp != null && itemEntriesProp.isArray)
        {
            for (int i = 0; i < itemEntriesProp.arraySize; i++)
            {
                SerializedProperty element = itemEntriesProp.GetArrayElementAtIndex(i);
                SerializedProperty itemIDProp = element.FindPropertyRelative("ItemID");
                SerializedProperty definitionProp = element.FindPropertyRelative("Definition");

                if (itemIDProp != null && !string.IsNullOrEmpty(itemIDProp.stringValue))
                {
                    ItemData data = new ItemData { id = itemIDProp.stringValue, name = "" };

                    if (definitionProp != null && definitionProp.objectReferenceValue != null)
                    {
                        ItemDefinition def = definitionProp.objectReferenceValue as ItemDefinition;
                        data.definition = def;
                        if (def.icon != null)
                        {
                            data.icon = def.icon.texture;
                        }
                        data.name = def.itemName;
                    }

                    _cachedItems.Add(data);
                }
            }
        }
    }
}
