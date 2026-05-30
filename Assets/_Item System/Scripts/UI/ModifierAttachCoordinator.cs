using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ModifierAttachCoordinator : MonoBehaviour
{
    public static event Action AttachmentCompleted;
    public static event Action<bool> TargetSelectionAvailabilityChanged;

    public static bool HasSelectedTarget { get; private set; }

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GridToggleGroup modifierGroup;
    [SerializeField] private GridToggleGroup targetGroup;

    private AttachGridSlot _selectedModifier;
    private AttachGridSlot _selectedTarget;

    private void Awake()
    {
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }
    }

    private void OnEnable()
    {
        AttachGridSlot.SelectionChanged += HandleSelectionChanged;
        AttachGridSlot.AttachRequested += HandleAttachRequested;
    }

    private void OnDisable()
    {
        AttachGridSlot.SelectionChanged -= HandleSelectionChanged;
        AttachGridSlot.AttachRequested -= HandleAttachRequested;
        ClearSelections();
    }

    private void HandleSelectionChanged(AttachGridSlot slot, bool isSelected)
    {
        if (slot.Role == AttachGridSlotRole.Modifier)
        {
            HandleModifierSelectionChanged(slot, isSelected);
            return;
        }

        HandleTargetSelectionChanged(slot, isSelected);
    }

    private void HandleModifierSelectionChanged(AttachGridSlot slot, bool isSelected)
    {
        if (isSelected)
        {
            _selectedModifier = slot;
            return;
        }

        if (_selectedModifier == slot)
        {
            _selectedModifier = null;
        }
    }

    private void HandleTargetSelectionChanged(AttachGridSlot slot, bool isSelected)
    {
        if (isSelected)
        {
            _selectedTarget = slot;
            SetTargetSelectionAvailability(true);
            return;
        }

        if (_selectedTarget == slot)
        {
            _selectedTarget = null;
            SetTargetSelectionAvailability(false);
        }
    }

    private void HandleAttachRequested(AttachGridSlot modifierSlot)
    {
        if (modifierSlot == null || _selectedTarget == null)
        {
            return;
        }

        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("ModifierAttachCoordinator: No PlayerInventory found.");
            return;
        }

        ItemRuntime targetRuntime = _selectedTarget.Runtime;
        string modifierId = modifierSlot.ModifierId;

        if (targetRuntime == null || string.IsNullOrWhiteSpace(modifierId))
        {
            Debug.LogWarning("ModifierAttachCoordinator: Missing target item or modifier id.");
            return;
        }

        ModifierRuntime attachedModifier = playerInventory.AttachModifierToItem(targetRuntime, modifierId);
        if (attachedModifier == null)
        {
            Debug.LogWarning($"ModifierAttachCoordinator: Failed to attach modifier '{modifierId}'.");
            return;
        }

        _selectedModifier = modifierSlot;
        ClearSelections();
        AttachmentCompleted?.Invoke();
    }

    private void ClearSelections()
    {
        AttachGridSlot modifierSlot = _selectedModifier;
        AttachGridSlot targetSlot = _selectedTarget;

        _selectedModifier = null;
        _selectedTarget = null;
        SetTargetSelectionAvailability(false);

        if (modifierGroup != null)
        {
            modifierGroup.ClearSelection();
        }
        else if (modifierSlot != null)
        {
            modifierSlot.SetToggled(false);
        }

        if (targetGroup != null)
        {
            targetGroup.ClearSelection();
        }
        else if (targetSlot != null)
        {
            targetSlot.SetToggled(false);
        }
    }

    private static void SetTargetSelectionAvailability(bool hasSelectedTarget)
    {
        if (HasSelectedTarget == hasSelectedTarget)
        {
            return;
        }

        HasSelectedTarget = hasSelectedTarget;
        TargetSelectionAvailabilityChanged?.Invoke(hasSelectedTarget);
    }

    public void ClearBoardSelection()
    {
        ClearSelections();
    }
}
