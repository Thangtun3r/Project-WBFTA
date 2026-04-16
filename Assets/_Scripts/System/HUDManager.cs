using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Volume postProcessVolume;

    [Header("Money Display")]
    [SerializeField] private float moneyChangeSpeed = 120f;

    [Header("Vignette Settings")]
    [SerializeField] private float damageFlashIntensity = 0.6f;
    [SerializeField] private float damageFlashDuration = 0.3f;
    [SerializeField] private float lowHealthVignetteIntensity = 0.4f;
    [SerializeField] private float lowHealthThreshold = 0.3f; // Trigger vignette when health is below 30%
    [SerializeField] private float vignetteTransitionSpeed = 2f;

    private float _maxHealth;
    private float _currentHealth;
    private Vignette _vignette;
    private float _targetVignetteIntensity;
    private float _damageFlashTimer;

    private int _displayedMoney;
    private int _targetMoney;

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHealthDisplay;
        GameManager.OnTimeUpdated += UpdateTimeDisplay;
        GameManager.OnLevelChanged += UpdateLevelDisplay;
        EconomyManager.OnMoneyChanged += UpdateMoneyDisplay;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHealthDisplay;
        GameManager.OnTimeUpdated -= UpdateTimeDisplay;
        GameManager.OnLevelChanged -= UpdateLevelDisplay;
        EconomyManager.OnMoneyChanged -= UpdateMoneyDisplay;
    }

    private void Start()
    {
        if (postProcessVolume != null)
        {
            if (postProcessVolume.profile.TryGet<Vignette>(out var vignette))
            {
                _vignette = vignette;
            }
        }
    }

    private void Update()
    {
        if (moneyText != null && _displayedMoney != _targetMoney)
        {
            _displayedMoney = (int)Mathf.MoveTowards(_displayedMoney, _targetMoney, moneyChangeSpeed * Time.deltaTime);
            moneyText.text = $"${_displayedMoney:F0}";
        }

        if (_vignette == null)
            return;

        // Handle damage flash timer
        if (_damageFlashTimer > 0)
        {
            _damageFlashTimer -= Time.deltaTime;
        }

        // Smoothly transition vignette intensity to target value
        float currentIntensity = _vignette.intensity.value;
        _vignette.intensity.value = Mathf.Lerp(currentIntensity, _targetVignetteIntensity, vignetteTransitionSpeed * Time.deltaTime);
    }

    private void UpdateHealthDisplay(float currentHealth, float maxHealth, bool isHealing)
    {
        _currentHealth = currentHealth;
        _maxHealth = maxHealth;

        // Update the fill image (0 to 1)
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = currentHealth / maxHealth;
        }

        // Update the text display (current / max)
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }

        // Handle vignette effects
        if (_vignette != null)
        {
            if (!isHealing)
            {
                // Damage taken: flash vignette
                _vignette.intensity.value = damageFlashIntensity;
                _damageFlashTimer = damageFlashDuration;
            }

            // Update target vignette intensity based on health
            float healthPercent = currentHealth / maxHealth;
            if (healthPercent <= lowHealthThreshold)
            {
                _targetVignetteIntensity = Mathf.Clamp01(lowHealthVignetteIntensity * ((lowHealthThreshold - healthPercent) / lowHealthThreshold));
            }
            else
            {
                _targetVignetteIntensity = 0f;
            }
        }
    }

    private void UpdateTimeDisplay(float timeElapsed)
    {
        if (timeText != null)
        {
            int minutes = (int)(timeElapsed / 60f);
            int seconds = (int)(timeElapsed % 60f);
            int milliseconds = (int)((timeElapsed % 1f) * 100f);

            timeText.text = $"{minutes:D2}:{seconds:D2}:{milliseconds:D2}";
        }
    }

    private void UpdateMoneyDisplay(int currentMoney)
    {
        _targetMoney = currentMoney;

        if (_displayedMoney == _targetMoney && moneyText != null)
        {
            moneyText.text = $"${currentMoney:F0}";
        }
    }

    private void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
        {
            levelText.text = $" {level}";
        }
    }
}
