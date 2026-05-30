using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemRuntimeVisual : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private GameObject modifierRoot;
    [SerializeField] private TMP_Text modifierCountText;
    [SerializeField] private AttachGridSlot attachTargetSlot;

    private ItemRuntime runtime;

    public ItemRuntime Runtime => runtime;

    private void Awake()
    {
        RefreshModifierRoot(false);
    }

    public void SetData(ItemRuntime itemRuntime)
    {
        runtime = itemRuntime;
        Refresh();
    }

    public void Refresh()
    {
        if (runtime == null)
        {
            RefreshAttachTargetSlot();
            RefreshModifierRoot(false);
            return;
        }

        ItemDefinition definition = runtime.Definition;

        if (iconImage != null)
        {
            iconImage.sprite = definition != null ? definition.icon : null;
        }

        if (nameText != null)
        {
            nameText.text = definition != null ? definition.itemName : string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = definition != null ? definition.description : string.Empty;
        }

        if (stackText != null)
        {
            stackText.text = runtime.StackSize.ToString();
        }

        int modifierCount = runtime.Modifiers != null ? runtime.Modifiers.Count : 0;
        RefreshModifierRoot(modifierCount > 0);
        RefreshAttachTargetSlot();

        if (modifierCountText != null)
        {
            modifierCountText.text = modifierCount.ToString();
        }
    }

    private void RefreshModifierRoot(bool hasModifiers)
    {
        if (modifierRoot != null)
        {
            modifierRoot.SetActive(hasModifiers);
        }
    }

    private void RefreshAttachTargetSlot()
    {
        if (attachTargetSlot != null)
        {
            attachTargetSlot.Bind(runtime);
        }
    }
}
