using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LogicBoardManager : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private GameObject boardRoot;
    [SerializeField] private KeyCode toggleBoardKey = KeyCode.Tab;
    [SerializeField] private bool startOpen;

    [Header("Inventory")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject itemTargetSlotPrefab;
    [SerializeField] private Transform itemTargetGridParent;
    [SerializeField] [Min(0)] private int randomItemSlotCount = 3;
    [SerializeField] private ModifierAttachCoordinator attachCoordinator;

    [Header("Random Modifiers")]
    [SerializeField] private GameObject modifierSlotPrefab;
    [SerializeField] private Transform modifierGridParent;
    [SerializeField] [Min(0)] private int randomModifierSlotCount = 3;
    [SerializeField] private bool rerollModifiersAfterAttach = true;

    [Header("Input")]
    [SerializeField] private KeyCode rerollModifierKey = KeyCode.R;
    [SerializeField] private KeyCode refreshInventoryKey = KeyCode.Space;

    [Header("Spawn Tween")]
    [SerializeField] [Min(0f)] private float spawnStagger = 0.06f;

    private readonly List<GameObject> _itemTargetObjects = new List<GameObject>();
    private readonly List<AttachGridSlot> _modifierSlots = new List<AttachGridSlot>();
    private readonly HashSet<ItemRuntime> _knownActiveItems = new HashSet<ItemRuntime>();
    private bool _subscribedToInventory;
    private bool _isOpen;

    private void Awake()
    {
        if (itemTargetGridParent == null)
        {
            itemTargetGridParent = transform;
        }

        if (modifierGridParent == null)
        {
            modifierGridParent = transform;
        }

        EnsureAttachCoordinator();

        ResolvePlayerInventory();
    }

    private void OnEnable()
    {
        ResolvePlayerInventory();
        SubscribeToInventoryUpdates();
        ModifierAttachCoordinator.AttachmentCompleted += HandleAttachmentCompleted;
    }

    private void Start()
    {
        ResolvePlayerInventory();
        SubscribeToInventoryUpdates();
        SetBoardOpen(startOpen);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleBoardKey))
        {
            SetBoardOpen(!_isOpen);
        }

        if (!_isOpen)
        {
            return;
        }

        if (Input.GetKeyDown(rerollModifierKey))
        {
            RebuildModifierSlots();
        }

        if (Input.GetKeyDown(refreshInventoryKey))
        {
            RefreshRandomItemTargets();
        }
    }

    private void OnDisable()
    {
        if (_subscribedToInventory && playerInventory != null)
        {
            playerInventory.InventoryUpdated -= HandleInventoryUpdated;
        }

        _subscribedToInventory = false;
        ModifierAttachCoordinator.AttachmentCompleted -= HandleAttachmentCompleted;
    }

    public void RefreshRandomItemTargets()
    {
        ClearItemTargets();
        ResolvePlayerInventory();

        IReadOnlyList<ItemRuntime> activeItems = playerInventory != null
            ? playerInventory.GetActiveItems()
            : null;

        List<ItemRuntime> candidates = BuildValidItemCandidates(activeItems);
        UpdateKnownActiveItems(candidates);
        if (candidates.Count == 0 || itemTargetSlotPrefab == null)
        {
            return;
        }

        int count = Mathf.Min(randomItemSlotCount, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            int candidateIndex = Random.Range(0, candidates.Count);
            ItemRuntime runtime = candidates[candidateIndex];
            candidates.RemoveAt(candidateIndex);

            GameObject slotObject = Instantiate(itemTargetSlotPrefab, itemTargetGridParent);
            ItemRuntimeVisual visual = slotObject.GetComponent<ItemRuntimeVisual>();
            AttachGridSlot slot = slotObject.GetComponent<AttachGridSlot>();
            if (visual == null || slot == null || slot.Role != AttachGridSlotRole.ItemTarget)
            {
                Debug.LogWarning("LogicBoardManager: Item target prefab must contain ItemRuntimeVisual and an ItemTarget-role AttachGridSlot.");
                Destroy(slotObject);
                continue;
            }

            visual.SetData(runtime);
            slot.PlaySpawnTween(i * spawnStagger);
            _itemTargetObjects.Add(slotObject);
        }
    }

    public void SetBoardOpen(bool isOpen)
    {
        _isOpen = isOpen;
        ResetBoard();

        if (boardRoot != null)
        {
            boardRoot.SetActive(isOpen);
        }

        if (!isOpen)
        {
            return;
        }

        RefreshRandomItemTargets();
        RebuildModifierSlots();
    }

    public void RebuildModifierSlots()
    {
        ClearModifierSlots();

        if (modifierSlotPrefab == null)
        {
            Debug.LogWarning("LogicBoardManager: Modifier slot prefab is not assigned.");
            return;
        }

        for (int i = 0; i < randomModifierSlotCount; i++)
        {
            GameObject slotObject = Instantiate(modifierSlotPrefab, modifierGridParent);
            AttachGridSlot slot = slotObject.GetComponent<AttachGridSlot>();
            if (slot == null || slot.Role != AttachGridSlotRole.Modifier)
            {
                Debug.LogWarning("LogicBoardManager: Modifier slot prefab must contain a Modifier-role AttachGridSlot.");
                Destroy(slotObject);
                continue;
            }

            slot.RollRandomModifier();
            slot.PlaySpawnTween(i * spawnStagger);
            _modifierSlots.Add(slot);
        }
    }

    public void RerollModifierSlots()
    {
        RebuildModifierSlots();
    }

    private void HandleAttachmentCompleted()
    {
        if (rerollModifiersAfterAttach)
        {
            RebuildModifierSlots();
        }
    }

    private void HandleInventoryUpdated(ItemRuntime runtime)
    {
        IReadOnlyList<ItemRuntime> activeItems = playerInventory != null
            ? playerInventory.GetActiveItems()
            : null;

        List<ItemRuntime> candidates = BuildValidItemCandidates(activeItems);
        if (!_isOpen)
        {
            UpdateKnownActiveItems(candidates);
            return;
        }

        if (!KnownActiveItemsMatch(candidates))
        {
            RefreshRandomItemTargets();
            return;
        }

        RefreshDisplayedItem(runtime);
    }

    private void ResolvePlayerInventory()
    {
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }
    }

    private void EnsureAttachCoordinator()
    {
        if (attachCoordinator != null)
        {
            return;
        }

        attachCoordinator = FindObjectOfType<ModifierAttachCoordinator>();
        if (attachCoordinator == null)
        {
            attachCoordinator = gameObject.AddComponent<ModifierAttachCoordinator>();
        }
    }

    private void SubscribeToInventoryUpdates()
    {
        if (_subscribedToInventory || playerInventory == null)
        {
            return;
        }

        playerInventory.InventoryUpdated += HandleInventoryUpdated;
        _subscribedToInventory = true;
    }

    private void ClearItemTargets()
    {
        attachCoordinator?.ClearBoardSelection();

        for (int i = 0; i < _itemTargetObjects.Count; i++)
        {
            if (_itemTargetObjects[i] != null)
            {
                Destroy(_itemTargetObjects[i]);
            }
        }

        _itemTargetObjects.Clear();
    }

    private void ClearModifierSlots()
    {
        for (int i = 0; i < _modifierSlots.Count; i++)
        {
            if (_modifierSlots[i] != null)
            {
                Destroy(_modifierSlots[i].gameObject);
            }
        }

        _modifierSlots.Clear();
    }

    private void ResetBoard()
    {
        ClearItemTargets();
        ClearModifierSlots();
        _knownActiveItems.Clear();
    }

    private static List<ItemRuntime> BuildValidItemCandidates(IReadOnlyList<ItemRuntime> activeItems)
    {
        List<ItemRuntime> candidates = new List<ItemRuntime>();
        if (activeItems == null)
        {
            return candidates;
        }

        for (int i = 0; i < activeItems.Count; i++)
        {
            ItemRuntime runtime = activeItems[i];
            if (runtime == null || runtime.Definition == null || runtime.StackSize <= 0)
            {
                continue;
            }

            if (runtime.Modifiers != null && runtime.Modifiers.Count > 0)
            {
                continue;
            }

            candidates.Add(runtime);
        }

        return candidates;
    }

    private void RefreshDisplayedItem(ItemRuntime runtime)
    {
        if (runtime == null)
        {
            return;
        }

        for (int i = 0; i < _itemTargetObjects.Count; i++)
        {
            GameObject slotObject = _itemTargetObjects[i];
            ItemRuntimeVisual visual = slotObject != null ? slotObject.GetComponent<ItemRuntimeVisual>() : null;
            if (visual != null && visual.Runtime == runtime)
            {
                visual.SetData(runtime);
                return;
            }
        }
    }

    private bool KnownActiveItemsMatch(IReadOnlyList<ItemRuntime> activeItems)
    {
        if (activeItems.Count != _knownActiveItems.Count)
        {
            return false;
        }

        for (int i = 0; i < activeItems.Count; i++)
        {
            if (!_knownActiveItems.Contains(activeItems[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateKnownActiveItems(IReadOnlyList<ItemRuntime> activeItems)
    {
        _knownActiveItems.Clear();
        for (int i = 0; i < activeItems.Count; i++)
        {
            _knownActiveItems.Add(activeItems[i]);
        }
    }
}
