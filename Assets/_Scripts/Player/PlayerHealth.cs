using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    public static event Action<float, float, bool> OnHealthChanged;

    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;
    [SerializeField] private PlayerInventory inventory;

    [Header("Regeneration Settings")]
    [SerializeField] private float healingDelay = 2f; // Wait this long after damage before healing starts
    [SerializeField] private float baseHealingRate = 10f; // Base health per second
    [SerializeField] private float healingExponent = 1.5f; // Exponential growth factor (higher = faster acceleration)

    private float _lastDamageTime = -Mathf.Infinity;
    private float _invincibleUntilTime = -Mathf.Infinity;
    private float _baseMaxHealth;
    private bool _initialized;
    private bool _subscribedToInventory;

    public bool IsInvincible => Time.time < _invincibleUntilTime;

    private void Awake()
    {
        EnsureInventory();
    }

    private void OnEnable()
    {
        SubscribeToInventory();
    }

    private void Start()
    {
        SubscribeToInventory();
    }

    private void OnDisable()
    {
        UnsubscribeFromInventory();
    }

    private void Update()
    {
        // Check if enough time has passed since last damage to start healing
        if (Time.time - _lastDamageTime >= healingDelay && _currentHealth < _maxHealth)
        {
            ApplyPassiveHealing();
        }
    }

    public void Initialize(float maxHealth)
    {
        _baseMaxHealth = maxHealth;
        _maxHealth = GetCalculatedMaxHealth();
        _currentHealth = _maxHealth;
        _initialized = true;
        _lastDamageTime = Time.time;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, false);
    }

    public void TakeDamage(float damage)
    {
        if (IsInvincible)
        {
            return;
        }

        _currentHealth -= damage;
        _lastDamageTime = Time.time; // Reset the healing delay timer
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, false);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void GrantInvincibility(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        _invincibleUntilTime = Mathf.Max(_invincibleUntilTime, Time.time + duration);
    }

    public virtual Transform GetTransform()
    {
        return transform;
    }

    /// <summary>
    /// Heals the player by a combination of raw amount and/or percentage of max health.
    /// </summary>
    /// <param name="rawAmount">Direct heal amount in HP. Default is 0.</param>
    /// <param name="percentageAmount">Heal amount as a percentage of max health (0-100). Default is 0.</param>
    public void Heal(float rawAmount = 0f, float percentageAmount = 0f)
    {
        float percentageHeal = _maxHealth * percentageAmount / 100f;
        float totalHealAmount = rawAmount + percentageHeal;
        _currentHealth = Mathf.Min(_currentHealth + totalHealAmount, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, true);
    }

    public void AddMaxHealthModifier(float amount)
    {
        _baseMaxHealth += amount;
        RecalculateMaxHealth(false);
    }

    private void ApplyPassiveHealing()
    {
        // Calculate time since damage (after the delay has passed)
        float timeSinceDamage = Time.time - _lastDamageTime - healingDelay;
        float healingMultiplier = 1f + Mathf.Pow(timeSinceDamage, healingExponent);
        
        float healThisFrame = baseHealingRate * healingMultiplier * Time.deltaTime;
        Heal(healThisFrame);
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Add death animation, game over screen logic, or respawn here
        // gameObject.SetActive(false);
    }

    private void HandleInventoryUpdated(ItemRuntime item)
    {
        RecalculateMaxHealth(true);
    }

    private void RecalculateMaxHealth(bool healIncrease)
    {
        if (!_initialized)
        {
            return;
        }

        float oldMaxHealth = _maxHealth;
        _maxHealth = GetCalculatedMaxHealth();

        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }

        float gainedHealth = _maxHealth - oldMaxHealth;
        if (healIncrease && gainedHealth > 0f)
        {
            _currentHealth = Mathf.Min(_currentHealth + gainedHealth, _maxHealth);
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, true);
    }

    private float GetCalculatedMaxHealth()
    {
        return inventory != null && inventory.ItemContext != null
            ? inventory.ItemContext.CalculatePlayerStat(PlayerStatType.MaxHealth, _baseMaxHealth)
            : _baseMaxHealth;
    }

    private void EnsureInventory()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>() ?? GetComponentInParent<PlayerInventory>();
        }
    }

    private void SubscribeToInventory()
    {
        EnsureInventory();
        if (_subscribedToInventory || inventory == null)
        {
            return;
        }

        inventory.InventoryUpdated += HandleInventoryUpdated;
        _subscribedToInventory = true;
    }

    private void UnsubscribeFromInventory()
    {
        if (!_subscribedToInventory || inventory == null)
        {
            return;
        }

        inventory.InventoryUpdated -= HandleInventoryUpdated;
        _subscribedToInventory = false;
    }
}
