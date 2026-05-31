using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemRuntimeVisual : MonoBehaviour
{
    private const float LiveRefreshInterval = 0.25f;

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private GameObject modifierRoot;
    [SerializeField] private TMP_Text modifierCountText;
    [SerializeField] private AttachGridSlot attachTargetSlot;

    private ItemRuntime runtime;
    private float _nextDescriptionRefreshTime;

    public ItemRuntime Runtime => runtime;

    private void Awake()
    {
        RefreshModifierRoot(false);
    }

    private void Update()
    {
        if (runtime == null || descriptionText == null || Time.unscaledTime < _nextDescriptionRefreshTime)
        {
            return;
        }

        _nextDescriptionRefreshTime = Time.unscaledTime + LiveRefreshInterval;
        descriptionText.text = DescriptionTokenResolver.Resolve(runtime);
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
            descriptionText.text = definition != null ? DescriptionTokenResolver.Resolve(runtime) : string.Empty;
            _nextDescriptionRefreshTime = Time.unscaledTime + LiveRefreshInterval;
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
