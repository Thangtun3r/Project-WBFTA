using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class TweenGridSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private GridToggleGroup _toggleGroup;
    private RectTransform _visualRoot;
    private RectTransform _buttonTarget;

    private readonly float _hoverScale = 1.2f;
    private readonly float _hoverDuration = 0.08f;
    private readonly Ease _hoverEase = Ease.OutBack;
    private readonly Ease _normalEase = Ease.OutQuad;

    private readonly Vector2 _toggledVisualOffset = new Vector2(0f, 28f);
    private readonly Vector2 _toggledButtonOffset = new Vector2(0f, -70f);
    private readonly float _toggleDuration = 0.06f;
    private readonly Ease _toggleEase = Ease.OutBack;

    private Vector3 _defaultScale = Vector3.one;
    private Vector2 _visualStartAnchoredPosition;
    private Vector2 _buttonStartAnchoredPosition;
    private Tween _scaleTween;
    private Tween _visualPositionTween;
    private Tween _buttonTween;
    private bool _isHovered;
    private bool _isToggled;

    public bool IsToggled => _isToggled;

    protected virtual bool UsesButtonToggleOffset => true;
    protected virtual bool CanToggle => true;

    protected virtual void Awake()
    {
        _toggleGroup = GetComponentInParent<GridToggleGroup>();
        _visualRoot = FindChildRectTransform("Visual") ?? transform as RectTransform;
        _buttonTarget = FindChildRectTransform("Button") ?? _visualRoot;

        if (_visualRoot != null)
        {
            _defaultScale = _visualRoot.localScale;
            _visualStartAnchoredPosition = _visualRoot.anchoredPosition;
        }

        if (_buttonTarget != null)
        {
            _buttonStartAnchoredPosition = _buttonTarget.anchoredPosition;
        }
    }

    protected virtual void OnDisable()
    {
        ClearSelectionState();
        _toggleGroup?.NotifySlotDisabled(this);
        KillTweens();
        ResetVisualState();
    }

    protected virtual void OnDestroy()
    {
        ClearSelectionState();
        _toggleGroup?.NotifySlotDisabled(this);
        KillTweens();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        RefreshScaleState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        RefreshScaleState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanToggle)
        {
            return;
        }

        if (_toggleGroup != null)
        {
            _toggleGroup.HandleSlotClick(this);
        }
        else
        {
            SetToggled(!_isToggled);
        }

    }

    public void SetToggled(bool isToggled)
    {
        if (_isToggled == isToggled)
        {
            return;
        }

        _isToggled = isToggled;
        RefreshVisualPositionState();
        RefreshButtonState();
        RefreshScaleState();
        OnToggledChanged(isToggled);
    }

    public void PlaySpawnTween(float delay = 0f)
    {
        if (_visualRoot == null)
        {
            return;
        }

        _scaleTween?.Kill();
        _visualRoot.localScale = Vector3.zero;
        _scaleTween = _visualRoot
            .DOScale(_defaultScale, _hoverDuration)
            .SetDelay(Mathf.Max(0f, delay))
            .SetEase(_hoverEase)
            .SetUpdate(true);
    }

    protected virtual void OnToggledChanged(bool isToggled)
    {
    }

    private RectTransform FindChildRectTransform(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child as RectTransform : null;
    }

    private void RefreshScaleState()
    {
        if (_visualRoot == null)
        {
            return;
        }

        _scaleTween?.Kill();
        _scaleTween = _visualRoot
            .DOScale(_defaultScale * (_isHovered ? _hoverScale : 1f), _hoverDuration)
            .SetEase(_isHovered ? _hoverEase : _normalEase)
            .SetUpdate(true);
    }

    private void RefreshButtonState()
    {
        if (_buttonTarget == null || !UsesButtonToggleOffset)
        {
            return;
        }

        _buttonTween?.Kill();
        Vector2 targetPosition = _buttonStartAnchoredPosition;
        if (_isToggled)
        {
            targetPosition += _toggledButtonOffset;
        }

        _buttonTween = DOTween.To(
                () => _buttonTarget.anchoredPosition,
                value => _buttonTarget.anchoredPosition = value,
                targetPosition,
                _toggleDuration)
            .SetEase(_toggleEase)
            .SetUpdate(true);
    }

    private void RefreshVisualPositionState()
    {
        if (_visualRoot == null)
        {
            return;
        }

        _visualPositionTween?.Kill();
        _visualPositionTween = DOTween.To(
                () => _visualRoot.anchoredPosition,
                value => _visualRoot.anchoredPosition = value,
                _isToggled ? _visualStartAnchoredPosition + _toggledVisualOffset : _visualStartAnchoredPosition,
                _toggleDuration)
            .SetEase(_toggleEase)
            .SetUpdate(true);
    }

    private void KillTweens()
    {
        _scaleTween?.Kill();
        _scaleTween = null;

        _visualPositionTween?.Kill();
        _visualPositionTween = null;

        _buttonTween?.Kill();
        _buttonTween = null;

    }

    private void ClearSelectionState()
    {
        if (!_isToggled)
        {
            return;
        }

        _isToggled = false;
        OnToggledChanged(false);
    }

    private void ResetVisualState()
    {
        _isHovered = false;
        _isToggled = false;

        if (_visualRoot != null)
        {
            _visualRoot.localScale = _defaultScale;
            _visualRoot.anchoredPosition = _visualStartAnchoredPosition;
        }

        if (_buttonTarget != null)
        {
            _buttonTarget.anchoredPosition = _buttonStartAnchoredPosition;
        }
    }
}
