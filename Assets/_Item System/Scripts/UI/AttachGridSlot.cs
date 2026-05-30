using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AttachGridSlotRole
{
    Modifier,
    ItemTarget
}

[DisallowMultipleComponent]
public class AttachGridSlot : TweenGridSlot
{
    public static event Action<AttachGridSlot, bool> SelectionChanged;
    public static event Action<AttachGridSlot> AttachRequested;

    [SerializeField] private AttachGridSlotRole role;
    [SerializeField] private bool toggleInteractable = true;
    [SerializeField] private bool autoRollRandomModifier = true;
    [SerializeField] private string modifierId;

    private ItemRuntime _runtime;
    private bool _hasRolledModifier;
    private Button _attachButton;

    public AttachGridSlotRole Role => role;
    public string ModifierId
    {
        get
        {
            EnsureRandomModifier();
            return modifierId;
        }
    }

    public ItemRuntime Runtime => _runtime;
    public ModifierDefinition ModifierDefinition =>
        role == AttachGridSlotRole.Modifier && ModifierDatabaseFactory.Instance != null
            ? ModifierDatabaseFactory.Instance.GetDefinition(ModifierId)
            : null;

    public ItemDefinition ItemDefinition => _runtime != null ? _runtime.Definition : null;

    protected override bool UsesButtonToggleOffset => role == AttachGridSlotRole.Modifier;
    protected override bool CanToggle =>
        toggleInteractable &&
        (role != AttachGridSlotRole.ItemTarget || _runtime == null || _runtime.Modifiers.Count == 0);

    protected override void Awake()
    {
        base.Awake();
        Button childButton = GetComponentInChildren<Button>(true);
        if (role == AttachGridSlotRole.Modifier)
        {
            _attachButton = childButton;
            if (_attachButton != null)
            {
                _attachButton.onClick.AddListener(RequestAttach);
            }
        }
        else if (childButton != null)
        {
            childButton.gameObject.SetActive(false);
        }

        RefreshAttachButtonInteractable();
    }

    private void OnEnable()
    {
        ModifierAttachCoordinator.TargetSelectionAvailabilityChanged += HandleTargetSelectionAvailabilityChanged;
        RefreshAttachButtonInteractable();
    }

    protected override void OnDisable()
    {
        ModifierAttachCoordinator.TargetSelectionAvailabilityChanged -= HandleTargetSelectionAvailabilityChanged;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        if (_attachButton != null)
        {
            _attachButton.onClick.RemoveListener(RequestAttach);
        }

        base.OnDestroy();
    }

    private void Start()
    {
        EnsureRandomModifier();
    }

    public void Bind(ItemRuntime runtime)
    {
        _runtime = runtime;
    }

    public bool RollRandomModifier()
    {
        if (role != AttachGridSlotRole.Modifier || ModifierDatabaseFactory.Instance == null)
        {
            return false;
        }

        IReadOnlyList<ModifierDatabaseFactory.ModifierEntry> entries =
            ModifierDatabaseFactory.Instance.GetAllEntries();
        if (entries == null || entries.Count == 0)
        {
            return false;
        }

        List<string> validModifierIds = new List<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            ModifierDatabaseFactory.ModifierEntry entry = entries[i];
            if (entry != null && entry.Definition != null && !string.IsNullOrWhiteSpace(entry.ModifierID))
            {
                validModifierIds.Add(entry.ModifierID);
            }
        }

        if (validModifierIds.Count == 0)
        {
            return false;
        }

        modifierId = validModifierIds[UnityEngine.Random.Range(0, validModifierIds.Count)];
        _hasRolledModifier = true;
        return true;
    }

    protected override void OnToggledChanged(bool isToggled)
    {
        SelectionChanged?.Invoke(this, isToggled);
    }

    private void EnsureRandomModifier()
    {
        if (role == AttachGridSlotRole.Modifier && autoRollRandomModifier && !_hasRolledModifier)
        {
            RollRandomModifier();
        }
    }

    private void HandleTargetSelectionAvailabilityChanged(bool hasSelectedTarget)
    {
        RefreshAttachButtonInteractable();
    }

    private void RefreshAttachButtonInteractable()
    {
        if (role == AttachGridSlotRole.Modifier && _attachButton != null)
        {
            _attachButton.interactable = ModifierAttachCoordinator.HasSelectedTarget;
        }
    }

    private void RequestAttach()
    {
        if (role == AttachGridSlotRole.Modifier)
        {
            AttachRequested?.Invoke(this);
        }
    }
}
