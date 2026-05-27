using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Segmented Layout")]
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private LayoutElement healthLayout;
    [SerializeField] private LayoutElement shieldLayout;
    [SerializeField] private LayoutElement overhealLayout;
    [SerializeField] private LayoutElement healthGoneLayout;
    [SerializeField] private bool includeShieldAndOverhealInCapacity = true;

    [Header("Damage Damp Bar")]
    [SerializeField] private LayoutElement damageDampLayout;
    [SerializeField] private Image damageDampImage;
    [SerializeField] private Color damageDampColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float damageDampSpeed = 6f;

    [Header("Optional Fill Images")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image shieldFillImage;
    [SerializeField] private Image overhealFillImage;
    [SerializeField] private Image healthGoneFillImage;

    [Header("Optional Text")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Color healthTextColor = Color.white;
    [SerializeField] private Color shieldTextColor = new Color(0.35f, 0.8f, 1f);

    public float CurrentVisibleWidth { get; private set; }

    private float _dampedLossWidth;
    private float _previousVisibleWidth;
    private bool _hasRendered;

    private void OnEnable()
    {
        PlayerHealth.OnHealthStateChanged += Render;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthStateChanged -= Render;
    }

    private void Update()
    {
        if (!_hasRendered || damageDampLayout == null)
        {
            return;
        }

        _dampedLossWidth = Mathf.Lerp(
            _dampedLossWidth,
            0f,
            Mathf.Max(0f, damageDampSpeed) * Time.deltaTime);

        if (_dampedLossWidth < 0.1f)
        {
            _dampedLossWidth = 0f;
        }

        SetPreferredWidth(damageDampLayout, _dampedLossWidth);
    }

    private void Render(PlayerHealthState state, bool isHealing)
    {
        float baseMaxHealth = Mathf.Max(1f, state.BaseMaxHealth);
        float barWidth = GetBarWidth();
        float displayCapacity = GetDisplayCapacity(state, baseMaxHealth);

        float healthWidth = GetSegmentWidth(state.CurrentHealth, displayCapacity, barWidth);
        float shieldWidth = GetSegmentWidth(state.Shield, displayCapacity, barWidth);
        float overhealWidth = GetSegmentWidth(state.Overheal, displayCapacity, barWidth);
        float reservedWidth = GetSegmentWidth(state.ReservedHealth, displayCapacity, barWidth);
        CurrentVisibleWidth = healthWidth + shieldWidth + overhealWidth;

        RefreshDamageDampBar();
        _previousVisibleWidth = CurrentVisibleWidth;

        SetPreferredWidth(healthLayout, healthWidth);
        SetPreferredWidth(shieldLayout, shieldWidth);
        SetPreferredWidth(overhealLayout, overhealWidth);
        SetPreferredWidth(healthGoneLayout, reservedWidth);

        SetFillAmount(healthFillImage, state.CurrentHealth / baseMaxHealth);
        SetFillAmount(shieldFillImage, state.Shield / baseMaxHealth);
        SetFillAmount(overhealFillImage, state.Overheal / baseMaxHealth);
        SetFillAmount(healthGoneFillImage, state.ReservedHealth / baseMaxHealth);

        if (healthText != null)
        {
            string healthColor = ColorUtility.ToHtmlStringRGB(healthTextColor);
            string shieldColor = ColorUtility.ToHtmlStringRGB(shieldTextColor);
            string healthPart = $"<color=#{healthColor}>{state.CurrentHealth:F0}/{state.MaxHealth:F0}</color>";

            healthText.text = state.Shield > 0f
                ? $"{healthPart} <color=#{shieldColor}>(+{state.Shield:F0})</color>"
                : healthPart;
        }
    }

    private void RefreshDamageDampBar()
    {
        if (damageDampImage != null)
        {
            damageDampImage.color = damageDampColor;
        }

        if (!_hasRendered)
        {
            _dampedLossWidth = 0f;
            SetPreferredWidth(damageDampLayout, 0f);
            _hasRendered = true;
            return;
        }

        float lostWidth = _previousVisibleWidth - CurrentVisibleWidth;
        if (lostWidth > 0f)
        {
            _dampedLossWidth += lostWidth;
        }
        else if (lostWidth < 0f)
        {
            _dampedLossWidth = Mathf.Max(0f, _dampedLossWidth + lostWidth);
        }

        SetPreferredWidth(damageDampLayout, _dampedLossWidth);
    }

    private float GetBarWidth()
    {
        RectTransform root = barRoot != null ? barRoot : transform as RectTransform;
        return root != null ? Mathf.Max(0f, root.rect.width) : 0f;
    }

    private float GetDisplayCapacity(PlayerHealthState state, float baseMaxHealth)
    {
        if (!includeShieldAndOverhealInCapacity)
        {
            return baseMaxHealth;
        }

        return Mathf.Max(1f, baseMaxHealth + state.MaxShield + state.MaxOverheal);
    }

    private static float GetSegmentWidth(float value, float displayCapacity, float barWidth)
    {
        if (value <= 0f || displayCapacity <= 0f || barWidth <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(value / displayCapacity) * barWidth;
    }

    private static void SetPreferredWidth(LayoutElement layoutElement, float width)
    {
        if (layoutElement == null)
        {
            return;
        }

        layoutElement.preferredWidth = Mathf.Max(0f, width);
    }

    private static void SetFillAmount(Image image, float amount)
    {
        if (image == null)
        {
            return;
        }

        image.fillAmount = Mathf.Clamp01(amount);
    }
}
