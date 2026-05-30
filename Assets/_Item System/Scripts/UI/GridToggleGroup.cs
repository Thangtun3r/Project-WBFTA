using UnityEngine;

[DisallowMultipleComponent]
public class GridToggleGroup : MonoBehaviour
{
    private TweenGridSlot _activeSlot;

    public TweenGridSlot ActiveSlot => _activeSlot;

    public void HandleSlotClick(TweenGridSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        bool shouldToggleOn = !slot.IsToggled;

        if (shouldToggleOn)
        {
            if (_activeSlot != null && _activeSlot != slot)
            {
                _activeSlot.SetToggled(false);
            }

            _activeSlot = slot;
            slot.SetToggled(true);
            return;
        }

        if (_activeSlot == slot)
        {
            _activeSlot = null;
        }

        slot.SetToggled(false);
    }

    public void ClearSelection()
    {
        if (_activeSlot == null)
        {
            return;
        }

        TweenGridSlot slot = _activeSlot;
        _activeSlot = null;
        slot.SetToggled(false);
    }

    public void NotifySlotDisabled(TweenGridSlot slot)
    {
        if (_activeSlot == slot)
        {
            _activeSlot = null;
        }
    }
}
