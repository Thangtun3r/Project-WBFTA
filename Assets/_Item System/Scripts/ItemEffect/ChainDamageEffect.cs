using UnityEngine;
using DG.Tweening;

public class ChainDamageEffect : MonoBehaviour, IItemEffect
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float duration = 0.2f;

    public void ApplyEffect(EffectContext context)
    {
        // Set the start and end of the lightning/trail
        lineRenderer.SetPosition(0, context.Origin);
        lineRenderer.SetPosition(1, context.Destination);

        // Fade out using DOTween
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;
        
        DOTween.To(() => startColor, x => lineRenderer.startColor = x, new Color(startColor.r, startColor.g, startColor.b, 0f), duration);
        DOTween.To(() => endColor, x => lineRenderer.endColor = x, new Color(endColor.r, endColor.g, endColor.b, 0f), duration);

        // Auto-cleanup so you don't clutter the hierarchy
        Destroy(gameObject, duration);
    }

    public void RemoveEffect(EffectContext context)
    {
        // One-shot effects usually don't need removal logic
    }
}