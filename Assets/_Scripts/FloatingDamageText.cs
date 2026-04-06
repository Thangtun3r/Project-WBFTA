using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textElement;
    
    [Header("Animation Settings")]
    [SerializeField] private float floatHeight = 1.5f;
    [SerializeField] private float duration = 0.6f;

    public void Setup(float damage, bool isCrit)
    {
        // RESET (Important for pooling)
        transform.DOKill();
        textElement.DOKill();
        transform.localScale = Vector3.one;
        textElement.alpha = 1f;

        // Visuals
        textElement.text = Mathf.RoundToInt(damage).ToString();
        textElement.color = isCrit ? Color.red : Color.white;
        if (isCrit) transform.localScale = Vector3.one * 1.5f;

        // ANIMATION
        // Random horizontal drift direction
        float randomX = Random.Range(-0.7f, 0.7f);
        Vector3 targetPos = new Vector3(transform.position.x + randomX, transform.position.y + floatHeight, 0);

        // Move Up + Random Drift
        transform.DOMove(targetPos, duration).SetEase(Ease.OutQuart);

        // Fade and Disable
        textElement.DOFade(0, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => gameObject.SetActive(false));
    }
}