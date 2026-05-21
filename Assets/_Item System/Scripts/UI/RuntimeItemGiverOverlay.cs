using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RuntimeItemGiverOverlay : MonoBehaviour
{
    [Header("Toggle")]
    private KeyCode toggleKey = KeyCode.F9;
    [SerializeField] private bool startOpen;
    [SerializeField] private bool showDebugButton = true;
    [SerializeField] private Rect debugButtonRect = new Rect(12f, 12f, 96f, 32f);

    [Header("Window")]
    [SerializeField] private Rect windowRect = new Rect(60f, 60f, 760f, 560f);
    [SerializeField] private float dragHandleHeight = 40f;
    [SerializeField] [Range(0f, 1f)] private float backgroundOpacityBoost = 0.2f;
    [SerializeField] private int defaultQuantity = 1;
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private int maxQuantity = 100;
    [SerializeField] private float buttonSize = 84f;
    [SerializeField] private float itemPadding = 8f;

    [Header("Pause")]
    [SerializeField] private bool pauseGameWhileOpen = true;

    private bool _isOpen;
    private int _quantity;
    private int _selectedTab;
    private int _selectedItemIndex;
    private int _selectedRuntimeItemIndex;
    private int _selectedModifierIndex;
    private Vector2 _itemScrollPosition;
    private Vector2 _runtimeItemScrollPosition;
    private Vector2 _modifierScrollPosition;
    private Vector2 _attachedModifierScrollPosition;
    private PlayerInventory _targetInventory;
    private List<ItemData> _cachedItems = new List<ItemData>();
    private List<ModifierData> _cachedModifiers = new List<ModifierData>();
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;
    private bool _cursorOverrideActive;
    private float _previousTimeScale = 1f;
    private bool _timeScaleOverrideActive;

    private static readonly string[] Tabs = { "Items", "Modifiers" };

    private struct ItemData
    {
        public string Id;
        public string Name;
        public string Description;
        public Sprite Icon;
    }

    private struct ModifierData
    {
        public string Id;
        public string Name;
        public string Description;
        public ModifierDefinition Definition;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<RuntimeItemGiverOverlay>() != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject(nameof(RuntimeItemGiverOverlay));
        DontDestroyOnLoad(overlayObject);
        overlayObject.AddComponent<RuntimeItemGiverOverlay>();
    }
#endif

    private void Awake()
    {
        _quantity = Mathf.Clamp(defaultQuantity, minQuantity, maxQuantity);
        SetOpen(startOpen);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetOpen(!_isOpen);
        }

        UpdateCursorState();
    }

    private void OnDisable()
    {
        RestoreCursorState();
        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        RestoreCursorState();
        RestoreTimeScale();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (showDebugButton)
        {
            string buttonLabel = _isOpen ? "Close Items" : "Open Items";
            if (GUI.Button(debugButtonRect, buttonLabel))
            {
                SetOpen(!_isOpen);
            }
        }

        if (!_isOpen)
        {
            return;
        }

        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "Runtime Item Tools");
        DrawTooltip();
    }

    private void DrawWindow(int windowId)
    {
        DrawBackgroundOverlay();
        EnsureTargetInventory();

        GUILayout.BeginVertical();
        DrawHeader();

        _selectedTab = GUILayout.Toolbar(_selectedTab, Tabs, GUILayout.Height(28f));
        GUILayout.Space(8f);

        if (_selectedTab == 0)
        {
            DrawItemsTab();
        }
        else
        {
            DrawModifiersTab();
        }

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, dragHandleHeight));
    }

    private void DrawHeader()
    {
        GUILayout.Label($"Toggle: {toggleKey}");

        if (_targetInventory == null)
        {
            GUILayout.Label("Target Inventory: none found");
        }
        else
        {
            GUILayout.Label($"Target Inventory: {_targetInventory.name}");
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Find Player Inventory", GUILayout.Height(24f)))
        {
            _targetInventory = FindObjectOfType<PlayerInventory>();
        }

        if (GUILayout.Button(_isOpen ? "Hide" : "Show", GUILayout.Width(80f), GUILayout.Height(24f)))
        {
            SetOpen(!_isOpen);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawItemsTab()
    {
        _quantity = Mathf.Clamp(
            Mathf.RoundToInt(GUILayout.HorizontalSlider(_quantity, minQuantity, maxQuantity)),
            minQuantity,
            maxQuantity);
        GUILayout.Label($"Quantity: {_quantity}");

        RefreshItems();
        if (_cachedItems.Count == 0)
        {
            GUILayout.Label("No items found in ItemDatabaseFactory.");
            return;
        }

        _itemScrollPosition = GUILayout.BeginScrollView(_itemScrollPosition);

        float availableWidth = Mathf.Max(1f, windowRect.width - 40f);
        int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (buttonSize + itemPadding)));
        int index = 0;

        while (index < _cachedItems.Count)
        {
            GUILayout.BeginHorizontal();

            for (int column = 0; column < columns && index < _cachedItems.Count; column++)
            {
                DrawItemButton(_cachedItems[index], index);
                index++;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        GUILayout.EndScrollView();
        DrawSelectedItemDetails();
    }

    private void DrawModifiersTab()
    {
        RefreshModifiers();

        if (_targetInventory == null)
        {
            GUILayout.Label("No PlayerInventory found.");
            return;
        }

        IReadOnlyList<ItemRuntime> activeItems = _targetInventory.GetActiveItems();
        _selectedRuntimeItemIndex = ClampIndex(_selectedRuntimeItemIndex, activeItems.Count);
        _selectedModifierIndex = ClampIndex(_selectedModifierIndex, _cachedModifiers.Count);

        GUILayout.BeginHorizontal();
        DrawActiveItemSelector(activeItems);
        DrawModifierSelector();
        DrawModifierActions(activeItems);
        GUILayout.EndHorizontal();
    }

    private void DrawActiveItemSelector(IReadOnlyList<ItemRuntime> activeItems)
    {
        GUILayout.BeginVertical(GUILayout.Width(230f));
        GUILayout.Label("Active Items");

        if (activeItems.Count == 0)
        {
            GUILayout.Label("No active items.");
            GUILayout.EndVertical();
            return;
        }

        _runtimeItemScrollPosition = GUILayout.BeginScrollView(_runtimeItemScrollPosition, GUILayout.Height(250f));
        for (int i = 0; i < activeItems.Count; i++)
        {
            ItemRuntime item = activeItems[i];
            bool wasSelected = i == _selectedRuntimeItemIndex;
            string label = GetRuntimeItemLabel(item);
            GUIContent content = new GUIContent(label, GetRuntimeItemTooltip(item));

            if (GUILayout.Toggle(wasSelected, content, "Button"))
            {
                _selectedRuntimeItemIndex = i;
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawModifierSelector()
    {
        GUILayout.BeginVertical(GUILayout.Width(230f));
        GUILayout.Label("Modifiers");

        if (ModifierDatabaseFactory.Instance == null)
        {
            GUILayout.Label("No ModifierDatabaseFactory found.");
            GUILayout.EndVertical();
            return;
        }

        if (_cachedModifiers.Count == 0)
        {
            GUILayout.Label("No modifiers found.");
            GUILayout.EndVertical();
            return;
        }

        _modifierScrollPosition = GUILayout.BeginScrollView(_modifierScrollPosition, GUILayout.Height(250f));
        for (int i = 0; i < _cachedModifiers.Count; i++)
        {
            ModifierData modifier = _cachedModifiers[i];
            bool wasSelected = i == _selectedModifierIndex;
            string label = string.IsNullOrEmpty(modifier.Name) ? modifier.Id : modifier.Name;
            GUIContent content = new GUIContent(label, BuildTooltip(label, modifier.Id, modifier.Description));

            if (GUILayout.Toggle(wasSelected, content, "Button"))
            {
                _selectedModifierIndex = i;
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawModifierActions(IReadOnlyList<ItemRuntime> activeItems)
    {
        GUILayout.BeginVertical(GUILayout.MinWidth(240f));
        GUILayout.Label("Selected");

        ItemRuntime selectedItem = GetSelectedRuntimeItem(activeItems);
        ModifierData? selectedModifier = GetSelectedModifier();

        GUILayout.Label($"Item: {GetRuntimeItemLabel(selectedItem)}");
        GUILayout.Label($"Modifier: {(selectedModifier.HasValue ? GetModifierLabel(selectedModifier.Value) : "none")}");
        DrawSelectedModifierDescription(selectedModifier);

        bool canUseModifier = selectedItem != null && selectedModifier.HasValue && selectedModifier.Value.Definition != null;
        bool hasModifier = canUseModifier && selectedItem.HasModifier(selectedModifier.Value.Definition);

        GUI.enabled = canUseModifier && !hasModifier;
        if (GUILayout.Button("Attach", GUILayout.Height(28f)))
        {
            _targetInventory.AttachModifierToItem(selectedItem, selectedModifier.Value.Id);
        }

        GUI.enabled = canUseModifier && hasModifier;
        if (GUILayout.Button("Remove", GUILayout.Height(28f)))
        {
            _targetInventory.RemoveModifierFromItem(selectedItem, selectedModifier.Value.Id);
        }
        GUI.enabled = true;

        GUILayout.Space(8f);
        GUILayout.Label("Attached Modifiers");
        DrawAttachedModifiers(selectedItem);
        GUILayout.EndVertical();
    }

    private void DrawAttachedModifiers(ItemRuntime selectedItem)
    {
        if (selectedItem == null)
        {
            GUILayout.Label("No item selected.");
            return;
        }

        IReadOnlyList<ModifierRuntime> modifiers = selectedItem.Modifiers;
        if (modifiers.Count == 0)
        {
            GUILayout.Label("No modifiers attached.");
            return;
        }

        _attachedModifierScrollPosition = GUILayout.BeginScrollView(_attachedModifierScrollPosition, GUILayout.Height(145f));
        for (int i = 0; i < modifiers.Count; i++)
        {
            ModifierRuntime modifier = modifiers[i];
            if (modifier == null || modifier.Definition == null)
            {
                continue;
            }

            GUILayout.BeginHorizontal();
            string label = GetModifierDefinitionLabel(modifier.Definition);
            GUILayout.Label(new GUIContent(label, BuildModifierTooltip(modifier.Definition)));
            if (GUILayout.Button("Remove", GUILayout.Width(72f)))
            {
                _targetInventory.RemoveModifierFromItem(selectedItem, modifier.Definition);
                break;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    private void DrawBackgroundOverlay()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, backgroundOpacityBoost);
        GUI.DrawTexture(new Rect(0f, 0f, windowRect.width, windowRect.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void DrawTooltip()
    {
        if (string.IsNullOrWhiteSpace(GUI.tooltip))
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            padding = new RectOffset(8, 8, 6, 6)
        };

        const float width = 300f;
        GUIContent content = new GUIContent(GUI.tooltip);
        float height = style.CalcHeight(content, width);
        Vector2 mousePosition = Event.current.mousePosition;
        Rect tooltipRect = new Rect(mousePosition.x + 16f, mousePosition.y + 16f, width, height);

        tooltipRect.x = Mathf.Min(tooltipRect.x, Screen.width - tooltipRect.width - 8f);
        tooltipRect.y = Mathf.Min(tooltipRect.y, Screen.height - tooltipRect.height - 8f);

        GUI.Box(tooltipRect, content, style);
    }

    private void DrawItemButton(ItemData item, int index)
    {
        GUILayout.BeginVertical(GUILayout.Width(buttonSize));

        Rect buttonRect = GUILayoutUtility.GetRect(buttonSize, buttonSize, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize));
        string itemTooltip = BuildTooltip(
            string.IsNullOrEmpty(item.Name) ? item.Id : item.Name,
            item.Id,
            item.Description);
        GUI.Box(buttonRect, new GUIContent(string.Empty, itemTooltip));

        if (item.Icon != null)
        {
            DrawSprite(buttonRect, item.Icon);
        }
        else
        {
            GUI.Label(buttonRect, item.Id);
        }

        if (Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition))
        {
            _selectedItemIndex = index;
            Event.current.Use();
        }

        string displayName = string.IsNullOrEmpty(item.Name) ? item.Id : item.Name;
        if (displayName.Length > 14)
        {
            displayName = displayName.Substring(0, 12) + "..";
        }

        GUILayout.Label(new GUIContent(displayName, itemTooltip), GUILayout.Width(buttonSize));

        GUILayout.BeginHorizontal(GUILayout.Width(buttonSize));
        if (GUILayout.Button("-", GUILayout.Width((buttonSize - 4f) * 0.5f), GUILayout.Height(22f)))
        {
            RemoveItem(item.Id);
        }

        if (GUILayout.Button("+", GUILayout.Width((buttonSize - 4f) * 0.5f), GUILayout.Height(22f)))
        {
            GiveItem(item.Id);
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawSelectedItemDetails()
    {
        if (_cachedItems.Count == 0)
        {
            return;
        }

        _selectedItemIndex = ClampIndex(_selectedItemIndex, _cachedItems.Count);
        ItemData item = _cachedItems[_selectedItemIndex];
        string name = string.IsNullOrEmpty(item.Name) ? item.Id : item.Name;

        GUILayout.Space(8f);
        GUILayout.Label(name);
        GUILayout.Label(string.IsNullOrWhiteSpace(item.Description) ? "No description." : item.Description);
    }

    private void DrawSelectedModifierDescription(ModifierData? selectedModifier)
    {
        if (!selectedModifier.HasValue)
        {
            GUILayout.Label("Description: none");
            return;
        }

        string description = selectedModifier.Value.Description;
        GUILayout.Label(string.IsNullOrWhiteSpace(description)
            ? "Description: none"
            : $"Description: {description}");
    }

    private void DrawSprite(Rect rect, Sprite sprite)
    {
        Rect paddedRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);

        GUI.DrawTextureWithTexCoords(paddedRect, sprite.texture, uv, true);
    }

    private void GiveItem(string itemId)
    {
        EnsureTargetInventory();
        if (_targetInventory == null)
        {
            Debug.LogWarning("RuntimeItemGiverOverlay: No PlayerInventory found.");
            return;
        }

        for (int i = 0; i < _quantity; i++)
        {
            _targetInventory.ProcessPickup(itemId);
        }

        Debug.Log($"Gave {_quantity} of {itemId} to {_targetInventory.name}");
    }

    private void RemoveItem(string itemId)
    {
        EnsureTargetInventory();
        if (_targetInventory == null)
        {
            Debug.LogWarning("RuntimeItemGiverOverlay: No PlayerInventory found.");
            return;
        }

        for (int i = 0; i < _quantity; i++)
        {
            _targetInventory.RemoveItem(itemId);
        }

        Debug.Log($"Removed {_quantity} of {itemId} from {_targetInventory.name}");
    }

    private void EnsureTargetInventory()
    {
        if (_targetInventory == null)
        {
            _targetInventory = FindObjectOfType<PlayerInventory>();
        }
    }

    private void RefreshItems()
    {
        ItemDatabaseFactory factory = ItemDatabaseFactory.Instance;
        _cachedItems.Clear();

        if (factory == null)
        {
            return;
        }

        IReadOnlyList<ItemDatabaseFactory.ItemEntry> entries = factory.GetAllEntries();
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemDatabaseFactory.ItemEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemID))
            {
                continue;
            }

            _cachedItems.Add(new ItemData
            {
                Id = entry.ItemID,
                Name = entry.Definition != null ? entry.Definition.itemName : string.Empty,
                Description = entry.Definition != null ? entry.Definition.description : string.Empty,
                Icon = entry.Definition != null ? entry.Definition.icon : null
            });
        }
    }

    private void RefreshModifiers()
    {
        ModifierDatabaseFactory factory = ModifierDatabaseFactory.Instance;
        _cachedModifiers.Clear();

        if (factory == null)
        {
            return;
        }

        IReadOnlyList<ModifierDatabaseFactory.ModifierEntry> entries = factory.GetAllEntries();
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ModifierDatabaseFactory.ModifierEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ModifierID))
            {
                continue;
            }

            _cachedModifiers.Add(new ModifierData
            {
                Id = entry.ModifierID,
                Name = entry.Definition != null ? entry.Definition.modifierName : string.Empty,
                Description = entry.Definition != null ? entry.Definition.description : string.Empty,
                Definition = entry.Definition
            });
        }
    }

    private static int ClampIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return Mathf.Clamp(index, 0, count - 1);
    }

    private static ItemRuntime GetSelectedRuntimeItem(IReadOnlyList<ItemRuntime> activeItems, int selectedIndex)
    {
        if (activeItems == null || activeItems.Count == 0 || selectedIndex < 0 || selectedIndex >= activeItems.Count)
        {
            return null;
        }

        return activeItems[selectedIndex];
    }

    private ItemRuntime GetSelectedRuntimeItem(IReadOnlyList<ItemRuntime> activeItems)
    {
        return GetSelectedRuntimeItem(activeItems, _selectedRuntimeItemIndex);
    }

    private ModifierData? GetSelectedModifier()
    {
        if (_cachedModifiers.Count == 0 || _selectedModifierIndex < 0 || _selectedModifierIndex >= _cachedModifiers.Count)
        {
            return null;
        }

        return _cachedModifiers[_selectedModifierIndex];
    }

    private string GetRuntimeItemLabel(ItemRuntime item)
    {
        if (item == null)
        {
            return "none";
        }

        string name = item.Definition != null && !string.IsNullOrEmpty(item.Definition.itemName)
            ? item.Definition.itemName
            : GetItemId(item.Definition);

        return $"{name} x{item.StackSize}";
    }

    private string GetRuntimeItemTooltip(ItemRuntime item)
    {
        if (item == null || item.Definition == null)
        {
            return string.Empty;
        }

        string name = !string.IsNullOrEmpty(item.Definition.itemName)
            ? item.Definition.itemName
            : GetItemId(item.Definition);

        return BuildTooltip(name, GetItemId(item.Definition), item.Definition.description);
    }

    private string GetItemId(ItemDefinition definition)
    {
        if (definition == null || ItemDatabaseFactory.Instance == null)
        {
            return "unknown";
        }

        IReadOnlyList<ItemDatabaseFactory.ItemEntry> entries = ItemDatabaseFactory.Instance.GetAllEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            ItemDatabaseFactory.ItemEntry entry = entries[i];
            if (entry != null && entry.Definition == definition)
            {
                return entry.ItemID;
            }
        }

        return "unknown";
    }

    private static string GetModifierLabel(ModifierData modifier)
    {
        return string.IsNullOrEmpty(modifier.Name) ? modifier.Id : modifier.Name;
    }

    private string GetModifierDefinitionLabel(ModifierDefinition definition)
    {
        if (definition == null)
        {
            return "unknown";
        }

        if (!string.IsNullOrEmpty(definition.modifierName))
        {
            return definition.modifierName;
        }

        if (!string.IsNullOrEmpty(definition.modifierId))
        {
            return definition.modifierId;
        }

        return definition.name;
    }

    private static string BuildModifierTooltip(ModifierDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        string id = !string.IsNullOrEmpty(definition.modifierId) ? definition.modifierId : definition.name;
        string name = !string.IsNullOrEmpty(definition.modifierName) ? definition.modifierName : id;
        return BuildTooltip(name, id, definition.description);
    }

    private static string BuildTooltip(string displayName, string id, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.IsNullOrWhiteSpace(id) ? displayName : $"{displayName}\n{id}";
        }

        return string.IsNullOrWhiteSpace(id)
            ? $"{displayName}\n{description}"
            : $"{displayName}\n{id}\n{description}";
    }

    private void SetOpen(bool isOpen)
    {
        if (_isOpen == isOpen)
        {
            return;
        }

        _isOpen = isOpen;
        UpdateCursorState();
        UpdateTimeScale();
    }

    private void UpdateCursorState()
    {
        if (_isOpen)
        {
            if (!_cursorOverrideActive)
            {
                _previousLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _cursorOverrideActive = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        RestoreCursorState();
    }

    private void RestoreCursorState()
    {
        if (!_cursorOverrideActive)
        {
            return;
        }

        Cursor.lockState = _previousLockMode;
        Cursor.visible = _previousCursorVisible;
        _cursorOverrideActive = false;
    }

    private void UpdateTimeScale()
    {
        if (_isOpen && pauseGameWhileOpen)
        {
            if (!_timeScaleOverrideActive)
            {
                _previousTimeScale = Time.timeScale;
                _timeScaleOverrideActive = true;
            }

            Time.timeScale = 0f;
            return;
        }

        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!_timeScaleOverrideActive)
        {
            return;
        }

        Time.timeScale = _previousTimeScale;
        _timeScaleOverrideActive = false;
    }
}
