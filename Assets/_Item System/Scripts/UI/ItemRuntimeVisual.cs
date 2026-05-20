using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemRuntimeVisual : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text stackText;

    private ItemRuntime runtime;

    public void SetData(ItemRuntime itemRuntime)
    {
        runtime = itemRuntime;
        Refresh();
    }

    public void Refresh()
    {
        if (runtime == null)
        {
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
    }
}
