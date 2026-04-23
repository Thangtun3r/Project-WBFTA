using UnityEngine;
using UnityEngine.UI;

public class HealthbarDamp : MonoBehaviour
{
    [SerializeField] private Image targetHealthImage;
    [SerializeField] private Image dampedHealthImage;
    [SerializeField] private float lerpSpeed = 2f;

    private float _dampedFillAmount;

    private void Start()
    {
        if (dampedHealthImage != null)
        {
            _dampedFillAmount = dampedHealthImage.fillAmount;
        }
    }

    private void Update()
    {
        if (targetHealthImage == null || dampedHealthImage == null)
            return;

        // Smoothly lerp the damped bar towards the target bar
        _dampedFillAmount = Mathf.Lerp(_dampedFillAmount, targetHealthImage.fillAmount, lerpSpeed * Time.deltaTime);
        dampedHealthImage.fillAmount = _dampedFillAmount;
    }
}

