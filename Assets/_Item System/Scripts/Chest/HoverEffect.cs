using UnityEngine;

public class HoverEffect : MonoBehaviour
{
    [SerializeField] private GameObject hoverEffectVisual;

    private void Awake()
    {
        if (hoverEffectVisual == null)
            hoverEffectVisual = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;

        if (hoverEffectVisual != null)
            hoverEffectVisual.SetActive(false);
    }

    private void OnEnable()
    {
        Chest.OnChestHover += HandleChestHover;
        Chest.OnChestExit += HandleChestExit;
        Chest.OnChestClick += HandleChestClick;
    }

    private void OnDisable()
    {
        Chest.OnChestHover -= HandleChestHover;
        Chest.OnChestExit -= HandleChestExit;
        Chest.OnChestClick -= HandleChestClick;
    }

    private void HandleChestHover(Transform chestTransform)
    {
        if (hoverEffectVisual == null)
            return;
        transform.position = chestTransform.position;

        hoverEffectVisual.SetActive(true);
    }

    private void HandleChestClick(Transform chestTransform)
    {
        if (hoverEffectVisual == null)
            return;
        transform.position = chestTransform.position;

        hoverEffectVisual.SetActive(true);
    }

    private void HandleChestExit(Transform chestTransform)
    {
        if (hoverEffectVisual == null)
            return;

        hoverEffectVisual.SetActive(false);
    }
}
