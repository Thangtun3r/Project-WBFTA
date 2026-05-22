using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Volume postProcessVolume;

    [Header("Vignette Settings")]
    [SerializeField] private float damageFlashIntensity = 0.6f;
    [SerializeField] private float damageFlashDuration = 0.3f;
    [SerializeField] private float lowHealthVignetteIntensity = 0.4f;
    [SerializeField] private float lowHealthThreshold = 0.3f; 
    [SerializeField] private float vignetteTransitionSpeed = 2f;

    private Vignette _vignette;
    private float _targetVignetteIntensity;
    private float _damageFlashTimer;

    private void OnEnable()
    {
        PlayerHealth.OnHealthStateChanged += UpdateHealthStateDisplay;
        GameManager.OnTimeUpdated += UpdateTimeDisplay;
        GameManager.OnLevelChanged += UpdateLevelDisplay;
        EconomyManager.OnMoneyChanged += UpdateMoneyDisplay;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthStateChanged -= UpdateHealthStateDisplay;
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
        if (_vignette == null)
            return;

        // Handle damage flash timer
        if (_damageFlashTimer > 0)
        {
            _damageFlashTimer -= Time.deltaTime;
        }

        // Smoothly transition vignette intensity
        float currentIntensity = _vignette.intensity.value;
        _vignette.intensity.value = Mathf.Lerp(currentIntensity, _targetVignetteIntensity, vignetteTransitionSpeed * Time.deltaTime);
    }

    private void UpdateHealthStateDisplay(PlayerHealthState state, bool isHealing)
    {
        UpdateVignette(state.CurrentHealth, state.MaxHealth, isHealing);
    }

    private void UpdateVignette(float currentHealth, float maxHealth, bool isHealing)
    {
        if (_vignette == null)
        {
            return;
        }

        if (!isHealing)
        {
            _vignette.intensity.value = damageFlashIntensity;
            _damageFlashTimer = damageFlashDuration;
        }

        float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        if (healthPercent <= lowHealthThreshold)
        {
            _targetVignetteIntensity = Mathf.Clamp01(lowHealthVignetteIntensity * ((lowHealthThreshold - healthPercent) / lowHealthThreshold));
        }
        else
        {
            _targetVignetteIntensity = 0f;
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
        // Money now updates instantly
        if (moneyText != null)
        {
            moneyText.text = $"${currentMoney}";
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
