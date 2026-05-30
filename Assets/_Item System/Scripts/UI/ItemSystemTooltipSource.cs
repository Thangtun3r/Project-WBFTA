using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ItemSystemTooltipSource : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private AttachGridSlot gridSlot;
    [SerializeField] private RectTransform anchorRect;
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private ModifierDefinition modifierDefinition;

    private void Awake()
    {
        if (gridSlot == null)
        {
            gridSlot = GetComponent<AttachGridSlot>();
        }

        if (anchorRect == null)
        {
            anchorRect = transform as RectTransform;
        }
    }

    private void OnDisable()
    {
        ItemSystemTooltip.Instance?.Hide(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemSystemTooltip tooltip = ItemSystemTooltip.Instance;
        if (tooltip == null)
        {
            return;
        }

        ModifierDefinition resolvedModifier = ResolveModifierDefinition();
        if (resolvedModifier != null)
        {
            tooltip.Show(resolvedModifier, anchorRect, this);
            return;
        }

        ItemRuntime resolvedRuntime = ResolveItemRuntime();
        if (resolvedRuntime != null)
        {
            tooltip.Show(resolvedRuntime, anchorRect, this);
            return;
        }

        ItemDefinition resolvedItem = ResolveItemDefinition();
        if (resolvedItem != null)
        {
            tooltip.Show(resolvedItem, anchorRect, this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemSystemTooltip.Instance?.Hide(this);
    }

    public void SetItemDefinition(ItemDefinition definition)
    {
        itemDefinition = definition;
    }

    public void SetModifierDefinition(ModifierDefinition definition)
    {
        modifierDefinition = definition;
    }

    private ItemDefinition ResolveItemDefinition()
    {
        return itemDefinition != null ? itemDefinition : gridSlot != null ? gridSlot.ItemDefinition : null;
    }

    private ItemRuntime ResolveItemRuntime()
    {
        return gridSlot != null ? gridSlot.Runtime : null;
    }

    private ModifierDefinition ResolveModifierDefinition()
    {
        return modifierDefinition != null
            ? modifierDefinition
            : gridSlot != null ? gridSlot.ModifierDefinition : null;
    }
}
