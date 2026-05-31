using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ItemSystemTooltip : MonoBehaviour
{
    private static readonly Regex DescriptionValuePattern =
        new Regex(@"(?<![\w])[-+]?\d+(?:\.\d+)?(?:%|x)?", RegexOptions.Compiled);

    public static ItemSystemTooltip Instance { get; private set; }

    [Header("Targets")]
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Image rarityTintImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statsText;

    [Header("Extended Modifiers")]
    [SerializeField] private GameObject extendedRoot;
    [SerializeField] private TMP_Text extendedTitleText;
    [SerializeField] private TMP_Text extendedInfoText;

    [Header("Position")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector2 cursorOffset = new Vector2(20f, 24f);
    [SerializeField] private float screenEdgePadding = 12f;

    [Header("Pop Tween")]
    [SerializeField] private float hiddenScale = 0.85f;
    [SerializeField] private float popDuration = 0.16f;
    [SerializeField] private Ease popEase = Ease.OutBack;

    [Header("Live Values")]
    [SerializeField] [Min(0.05f)] private float liveRefreshInterval = 0.25f;

    [Header("Text Colors")]
    [SerializeField] private Color statNameColor = new Color(0.56f, 0.84f, 1f);
    [SerializeField] private Color positiveValueColor = new Color(0.42f, 1f, 0.58f);
    [SerializeField] private Color negativeValueColor = new Color(1f, 0.46f, 0.46f);
    [SerializeField] private Color parameterColor = new Color(1f, 0.83f, 0.4f);

    private Vector3 _visibleScale = Vector3.one;
    private Tween _popTween;
    private object _currentSource;
    private ItemDefinition _currentItemDefinition;
    private ItemRuntime _currentItemRuntime;
    private ModifierDefinition _currentModifierDefinition;
    private float _nextLiveRefreshTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("ItemSystemTooltip: Multiple tooltip presenters found. Using the newest enabled instance.");
        }

        Instance = this;

        if (tooltipRoot == null)
        {
            tooltipRoot = transform as RectTransform;
        }

        if (tooltipRoot != null)
        {
            _visibleScale = tooltipRoot.localScale;
        }

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        _popTween?.Kill();
    }

    private void Update()
    {
        if (tooltipRoot == null || !tooltipRoot.gameObject.activeSelf || Time.unscaledTime < _nextLiveRefreshTime)
        {
            return;
        }

        _nextLiveRefreshTime = Time.unscaledTime + liveRefreshInterval;
        RefreshCurrentText();
    }

    public void Show(ItemDefinition definition, RectTransform sourceRect, object source = null)
    {
        if (definition == null)
        {
            return;
        }

        _currentSource = source;
        _currentItemDefinition = definition;
        _currentItemRuntime = null;
        _currentModifierDefinition = null;
        RefreshCurrentText();
        PositionAt(sourceRect);
        PlayPopTween();
    }

    public void Show(ItemRuntime runtime, RectTransform sourceRect, object source = null)
    {
        if (runtime == null || runtime.Definition == null)
        {
            return;
        }

        ItemDefinition definition = runtime.Definition;
        _currentSource = source;
        _currentItemDefinition = definition;
        _currentItemRuntime = runtime;
        _currentModifierDefinition = null;
        RefreshCurrentText();
        PositionAt(sourceRect);
        PlayPopTween();
    }

    public void Show(ModifierDefinition definition, RectTransform sourceRect, object source = null)
    {
        if (definition == null)
        {
            return;
        }

        _currentSource = source;
        _currentItemDefinition = null;
        _currentItemRuntime = null;
        _currentModifierDefinition = definition;
        RefreshCurrentText();
        PositionAt(sourceRect);
        PlayPopTween();
    }

    public void Hide(object source = null)
    {
        if (source != null && !ReferenceEquals(source, _currentSource))
        {
            return;
        }

        _currentSource = null;
        _currentItemDefinition = null;
        _currentItemRuntime = null;
        _currentModifierDefinition = null;
        _popTween?.Kill();
        _popTween = null;

        if (tooltipRoot != null)
        {
            tooltipRoot.gameObject.SetActive(false);
        }
    }

    private void RefreshCurrentText()
    {
        _nextLiveRefreshTime = Time.unscaledTime + liveRefreshInterval;

        if (_currentItemRuntime != null && _currentItemRuntime.Definition != null)
        {
            ItemDefinition definition = _currentItemRuntime.Definition;
            SetText(
                GetItemDisplayName(definition),
                definition.itemRarity,
                DescriptionTokenResolver.Resolve(_currentItemRuntime),
                BuildItemStats(_currentItemRuntime));
            SetExtendedModifiers(_currentItemRuntime.Modifiers);
            return;
        }

        if (_currentItemDefinition != null)
        {
            SetText(
                GetItemDisplayName(_currentItemDefinition),
                _currentItemDefinition.itemRarity,
                DescriptionTokenResolver.Resolve(_currentItemDefinition),
                BuildItemStats(_currentItemDefinition));
            return;
        }

        if (_currentModifierDefinition != null)
        {
            SetText(
                string.IsNullOrWhiteSpace(_currentModifierDefinition.modifierName)
                    ? _currentModifierDefinition.name
                    : _currentModifierDefinition.modifierName,
                _currentModifierDefinition.rarity,
                DescriptionTokenResolver.Resolve(_currentModifierDefinition),
                BuildModifierStats(_currentModifierDefinition));
        }
    }

    private static string GetItemDisplayName(ItemDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.itemName) ? definition.name : definition.itemName;
    }

    private void SetText(string displayName, ItemRarity rarity, string description, string stats)
    {
        SetExtendedModifiers(null);

        if (nameText != null)
        {
            nameText.text = displayName;
        }

        if (rarityText != null)
        {
            rarityText.text = rarity.ToString();
        }

        if (rarityTintImage != null)
        {
            rarityTintImage.color = GetRarityColor(rarity);
        }

        if (descriptionText != null)
        {
            descriptionText.text = HighlightDescriptionValues(description);
        }

        if (statsText != null)
        {
            statsText.text = stats;
        }
    }

    private void SetExtendedModifiers(IReadOnlyList<ModifierRuntime> modifiers)
    {
        StringBuilder titleBuilder = new StringBuilder("Extended with ");
        StringBuilder infoBuilder = new StringBuilder();
        int validModifierCount = 0;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierDefinition definition = modifiers[i]?.Definition;
                if (definition == null)
                {
                    continue;
                }

                string modifierName = string.IsNullOrWhiteSpace(definition.modifierName)
                    ? definition.name
                    : definition.modifierName;

                if (validModifierCount > 0)
                {
                    titleBuilder.Append(", ");
                    infoBuilder.AppendLine();
                }

                titleBuilder.Append(modifierName);
                infoBuilder
                    .Append("<b>")
                    .Append(modifierName)
                    .AppendLine("</b>");

                string description = HighlightDescriptionValues(DescriptionTokenResolver.Resolve(modifiers[i]));
                if (!string.IsNullOrWhiteSpace(description))
                {
                    infoBuilder.AppendLine(description);
                }

                infoBuilder.Append(BuildModifierStats(modifiers[i]));
                validModifierCount++;
            }
        }

        bool hasModifiers = validModifierCount > 0;
        if (extendedRoot != null)
        {
            extendedRoot.SetActive(hasModifiers);
        }

        if (extendedTitleText != null)
        {
            extendedTitleText.text = hasModifiers ? titleBuilder.ToString() : string.Empty;
        }

        if (extendedInfoText != null)
        {
            extendedInfoText.text = hasModifiers ? infoBuilder.ToString().TrimEnd() : string.Empty;
        }
    }

    private void PlayPopTween()
    {
        if (tooltipRoot == null)
        {
            return;
        }

        _popTween?.Kill();
        tooltipRoot.gameObject.SetActive(true);
        tooltipRoot.localScale = _visibleScale * hiddenScale;
        _popTween = tooltipRoot
            .DOScale(_visibleScale, popDuration)
            .SetEase(popEase)
            .SetUpdate(true)
            .OnComplete(() => _popTween = null)
            .OnKill(() => _popTween = null);
    }

    public void PositionAt(Vector2 screenPosition)
    {
        if (tooltipRoot == null)
        {
            return;
        }

        Canvas resolvedCanvas = canvas != null ? canvas : tooltipRoot.GetComponentInParent<Canvas>();
        Canvas positioningCanvas = resolvedCanvas != null && resolvedCanvas.rootCanvas != null
            ? resolvedCanvas.rootCanvas
            : resolvedCanvas;
        RectTransform canvasRect = positioningCanvas != null ? positioningCanvas.transform as RectTransform : null;
        if (canvasRect == null)
        {
            return;
        }

        tooltipRoot.gameObject.SetActive(true);

        Camera eventCamera = positioningCanvas != null && positioningCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? positioningCanvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 pointerPosition))
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);

        float verticalOffset = Mathf.Abs(cursorOffset.y);
        float tooltipWidth = GetCanvasSpaceSize(tooltipRoot.rect.width, tooltipRoot.lossyScale.x, canvasRect.lossyScale.x);
        float tooltipHeight = GetCanvasSpaceSize(tooltipRoot.rect.height, tooltipRoot.lossyScale.y, canvasRect.lossyScale.y);
        bool placeAbove = pointerPosition.y - verticalOffset - tooltipHeight < canvasRect.rect.yMin + screenEdgePadding;

        tooltipRoot.pivot = placeAbove ? new Vector2(0f, 0f) : new Vector2(0f, 1f);

        Vector2 targetPosition = pointerPosition + new Vector2(
            cursorOffset.x,
            placeAbove ? verticalOffset : -verticalOffset);

        Rect parentBounds = canvasRect.rect;
        targetPosition.x = Mathf.Clamp(
            targetPosition.x,
            parentBounds.xMin + screenEdgePadding,
            parentBounds.xMax - screenEdgePadding - tooltipWidth);
        targetPosition.y = Mathf.Clamp(
            targetPosition.y,
            parentBounds.yMin + screenEdgePadding + tooltipHeight * tooltipRoot.pivot.y,
            parentBounds.yMax - screenEdgePadding - tooltipHeight * (1f - tooltipRoot.pivot.y));

        tooltipRoot.position = canvasRect.TransformPoint(targetPosition);
    }

    public void PositionAt(RectTransform sourceRect)
    {
        if (sourceRect == null)
        {
            return;
        }

        Canvas sourceCanvas = sourceRect.GetComponentInParent<Canvas>();
        Canvas positioningCanvas = sourceCanvas != null && sourceCanvas.rootCanvas != null
            ? sourceCanvas.rootCanvas
            : sourceCanvas;
        Camera eventCamera = positioningCanvas != null && positioningCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? positioningCanvas.worldCamera
            : null;

        PositionAt(RectTransformUtility.WorldToScreenPoint(eventCamera, sourceRect.TransformPoint(sourceRect.rect.center)));
    }

    private void HideImmediate()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.gameObject.SetActive(false);
        }
    }

    private string BuildItemStats(ItemDefinition definition)
    {
        StringBuilder builder = new StringBuilder();
        AppendItemStats(builder, definition.itemStats);
        AppendPlayerStats(builder, definition.playerStats);
        AppendParameters(builder, definition.parameters);
        return builder.ToString();
    }

    private string BuildItemStats(ItemRuntime runtime)
    {
        StringBuilder builder = new StringBuilder();
        ItemDefinition definition = runtime.Definition;
        AppendRuntimeItemStats(builder, runtime, definition.itemStats);
        AppendRuntimePlayerStats(builder, runtime.StackSize, definition.playerStats);
        AppendRuntimeParameters(builder, runtime, definition.parameters);
        return builder.ToString();
    }

    private string BuildModifierStats(ModifierDefinition definition)
    {
        StringBuilder builder = new StringBuilder();
        AppendModifierStats(builder, definition.itemStatModifiers);
        AppendModifierStats(builder, definition.playerStatModifiers);
        AppendModifierParameters(builder, definition.parameterModifiers);
        AppendParameters(builder, definition.parameters);

        if (!Mathf.Approximately(definition.procCoefficient, 1f))
        {
            AppendLine(builder, "Proc Coefficient", FormatValue(definition.procCoefficient), positiveValueColor);
        }

        return builder.ToString();
    }

    private string BuildModifierStats(ModifierRuntime runtime)
    {
        ModifierDefinition definition = runtime.Definition;
        StringBuilder builder = new StringBuilder();
        AppendModifierStats(builder, definition.itemStatModifiers);
        AppendModifierStats(builder, definition.playerStatModifiers);
        AppendModifierParameters(builder, definition.parameterModifiers);
        AppendRuntimeModifierParameters(builder, runtime, definition.parameters);

        if (!Mathf.Approximately(definition.procCoefficient, 1f))
        {
            AppendLine(builder, "Proc Coefficient", FormatValue(definition.procCoefficient), positiveValueColor);
        }

        return builder.ToString();
    }

    private void AppendItemStats(StringBuilder builder, IReadOnlyList<ItemStatEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemStatEntry entry = entries[i];
            string value = FormatValue(entry.baseValue);
            if (!Mathf.Approximately(entry.perStackValue, 0f))
            {
                value += $" ({FormatSignedValue(entry.perStackValue)} per stack)";
            }

            AppendLine(builder, entry.statType.ToString(), value, GetValueColor(entry.baseValue));
        }
    }

    private void AppendPlayerStats(StringBuilder builder, IReadOnlyList<PlayerStatEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            PlayerStatEntry entry = entries[i];
            string value = FormatValue(entry.baseValue);
            if (!Mathf.Approximately(entry.perStackValue, 0f))
            {
                value += $" ({FormatSignedValue(entry.perStackValue)} per stack)";
            }

            AppendLine(builder, entry.statType.ToString(), value, GetValueColor(entry.baseValue));
        }
    }

    private void AppendRuntimeItemStats(StringBuilder builder, ItemRuntime runtime, IReadOnlyList<ItemStatEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemStatEntry entry = entries[i];
            float value = runtime.GetItemStat(entry.statType, entry.GetValue(runtime.StackSize));
            AppendLine(builder, entry.statType.ToString(), FormatValue(value), GetValueColor(value));
        }
    }

    private void AppendRuntimePlayerStats(StringBuilder builder, int stackSize, IReadOnlyList<PlayerStatEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            PlayerStatEntry entry = entries[i];
            float value = entry.GetValue(stackSize);
            AppendLine(builder, entry.statType.ToString(), FormatValue(value), GetValueColor(value));
        }
    }

    private void AppendParameters(StringBuilder builder, IReadOnlyList<ItemParameterEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemParameterEntry entry = entries[i];
            string value = FormatValue(entry.baseValue);
            if (!Mathf.Approximately(entry.perStackValue, 0f))
            {
                value += $" ({FormatSignedValue(entry.perStackValue)} per stack)";
            }

            AppendLine(builder, entry.key, value, parameterColor);
        }
    }

    private void AppendRuntimeParameters(StringBuilder builder, ItemRuntime runtime, IReadOnlyList<ItemParameterEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemParameterEntry entry = entries[i];
            float value = runtime.GetParameter(entry.key, entry.GetValue(runtime.StackSize));
            AppendLine(builder, entry.key, FormatValue(value), GetValueColor(value), parameterColor);
        }
    }

    private void AppendRuntimeModifierParameters(
        StringBuilder builder,
        ModifierRuntime runtime,
        IReadOnlyList<ItemParameterEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemParameterEntry entry = entries[i];
            float value = runtime.GetParameter(entry.key, entry.GetValue(runtime.AttachedItemStackSize));
            AppendLine(builder, entry.key, FormatValue(value), GetValueColor(value), parameterColor);
        }
    }

    private void AppendModifierStats(StringBuilder builder, IReadOnlyList<ItemStatModifierEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemStatModifierEntry entry = entries[i];
            AppendModifierLine(builder, entry.statType.ToString(), entry.flatBonus, entry.multiplierBonus);
        }
    }

    private void AppendModifierStats(StringBuilder builder, IReadOnlyList<PlayerStatModifierEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            PlayerStatModifierEntry entry = entries[i];
            AppendModifierLine(builder, entry.statType.ToString(), entry.flatBonus, entry.multiplierBonus);
        }
    }

    private void AppendModifierParameters(StringBuilder builder, IReadOnlyList<ItemParameterModifierEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ItemParameterModifierEntry entry = entries[i];
            AppendModifierLine(builder, entry.key, entry.flatBonus, entry.multiplierBonus, parameterColor);
        }
    }

    private void AppendModifierLine(
        StringBuilder builder,
        string label,
        float flatBonus,
        float multiplierBonus,
        Color? labelColor = null)
    {
        if (!Mathf.Approximately(flatBonus, 0f))
        {
            AppendLine(builder, label, FormatSignedValue(flatBonus), GetValueColor(flatBonus), labelColor);
        }

        if (!Mathf.Approximately(multiplierBonus, 0f))
        {
            AppendLine(builder, $"{label} multiplier", FormatSignedPercent(multiplierBonus), GetValueColor(multiplierBonus), labelColor);
        }
    }

    private void AppendLine(
        StringBuilder builder,
        string label,
        string value,
        Color valueColor,
        Color? labelColor = null)
    {
        builder
            .Append("<color=#")
            .Append(ColorUtility.ToHtmlStringRGB(labelColor ?? statNameColor))
            .Append('>')
            .Append(label)
            .Append(":</color> <color=#")
            .Append(ColorUtility.ToHtmlStringRGB(valueColor))
            .Append('>')
            .Append(value)
            .AppendLine("</color>");
    }

    private Color GetValueColor(float value)
    {
        return value < 0f ? negativeValueColor : positiveValueColor;
    }

    private static string FormatValue(float value)
    {
        return value.ToString("0.##");
    }

    private static string FormatSignedValue(float value)
    {
        return value >= 0f ? $"+{FormatValue(value)}" : FormatValue(value);
    }

    private static string FormatSignedPercent(float value)
    {
        float percent = value * 100f;
        return percent >= 0f ? $"+{FormatValue(percent)}%" : $"{FormatValue(percent)}%";
    }

    private static float GetCanvasSpaceSize(float rectSize, float tooltipScale, float canvasScale)
    {
        return Mathf.Approximately(canvasScale, 0f) ? rectSize : rectSize * tooltipScale / canvasScale;
    }

    private string HighlightDescriptionValues(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        string color = ColorUtility.ToHtmlStringRGB(positiveValueColor);
        return DescriptionValuePattern.Replace(description, match => $"<color=#{color}>{match.Value}</color>");
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Rare:
                return Color.green;
            case ItemRarity.Legendary:
                return Color.red;
            case ItemRarity.Common:
                return Color.blue;
            case ItemRarity.Uncommon:
                return new Color(0.35f, 0.85f, 0.35f);
            case ItemRarity.Epic:
                return new Color(0.8f, 0.35f, 1f);
            default:
                return Color.white;
        }
    }

}
