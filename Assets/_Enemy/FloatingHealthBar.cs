using UnityEngine;

public class FloatingHealthBar : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image healthBarFill;
    private IHealthObservable _target;

    private void Awake()
    {
        // Find the interface on the parent object
        _target = GetComponentInParent<IHealthObservable>();
        
        if (_target == null)
        {
            Debug.LogError("FloatingHealthBar: Could not find IHealthObservable on parent!");
        }
        
        if (healthBarFill == null)
        {
            Debug.LogError("FloatingHealthBar: healthBarFill is not assigned!");
        }
    }

    private void OnEnable()
    {
        if (_target != null)
        {
            _target.OnHealthChanged += UpdateFill;
            // Initialize the health bar with current values
            UpdateFill(_target.CurrentHealth, _target.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (_target != null) _target.OnHealthChanged -= UpdateFill;
    }

    private void UpdateFill(float current, float max)
    {
        if (healthBarFill == null) return;
        
        // Clamp fill amount between 0 and 1
        healthBarFill.fillAmount = Mathf.Clamp01(current / max);
    }
}