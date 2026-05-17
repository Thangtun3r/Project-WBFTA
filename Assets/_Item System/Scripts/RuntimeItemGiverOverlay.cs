using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RuntimeItemGiverOverlay : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    [SerializeField] private bool startOpen;
    [SerializeField] private bool showDebugButton = true;
    [SerializeField] private Rect debugButtonRect = new Rect(12f, 12f, 96f, 32f);

    [Header("Window")]
    [SerializeField] private Rect windowRect = new Rect(60f, 60f, 720f, 520f);
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
    private Vector2 _scrollPosition;
    private PlayerInventory _targetInventory;
    private List<ItemData> _cachedItems = new List<ItemData>();
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;
    private bool _cursorOverrideActive;
    private float _previousTimeScale = 1f;
    private bool _timeScaleOverrideActive;

    private struct ItemData
    {
        public string Id;
        public string Name;
        public Sprite Icon;
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

        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "Runtime Item Giver");
    }

    private void DrawWindow(int windowId)
    {
        DrawBackgroundOverlay();
        EnsureTargetInventory();

        GUILayout.BeginVertical();
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

        _quantity = Mathf.Clamp(
            Mathf.RoundToInt(GUILayout.HorizontalSlider(_quantity, minQuantity, maxQuantity)),
            minQuantity,
            maxQuantity);
        GUILayout.Label($"Quantity: {_quantity}");

        RefreshItems();
        if (_cachedItems.Count == 0)
        {
            GUILayout.Label("No items found in ItemDatabaseFactory.");
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, dragHandleHeight));
            return;
        }

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        float availableWidth = Mathf.Max(1f, windowRect.width - 40f);
        int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (buttonSize + itemPadding)));
        int index = 0;

        while (index < _cachedItems.Count)
        {
            GUILayout.BeginHorizontal();

            for (int column = 0; column < columns && index < _cachedItems.Count; column++)
            {
                DrawItemButton(_cachedItems[index]);
                index++;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, dragHandleHeight));
    }

    private void DrawBackgroundOverlay()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, backgroundOpacityBoost);
        GUI.DrawTexture(new Rect(0f, 0f, windowRect.width, windowRect.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void DrawItemButton(ItemData item)
    {
        GUILayout.BeginVertical(GUILayout.Width(buttonSize));

        Rect buttonRect = GUILayoutUtility.GetRect(buttonSize, buttonSize, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize));
        GUI.Box(buttonRect, GUIContent.none);

        if (item.Icon != null)
        {
            DrawSprite(buttonRect, item.Icon);
        }
        else
        {
            GUI.Label(buttonRect, item.Id);
        }

        string displayName = string.IsNullOrEmpty(item.Name) ? item.Id : item.Name;
        if (displayName.Length > 14)
        {
            displayName = displayName.Substring(0, 12) + "..";
        }

        GUILayout.Label(displayName, GUILayout.Width(buttonSize));

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
                Icon = entry.Definition != null ? entry.Definition.icon : null
            });
        }
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
