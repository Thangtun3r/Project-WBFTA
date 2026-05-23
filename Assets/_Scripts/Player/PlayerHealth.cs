using System;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerHealthState
{
    public float CurrentHealth;
    public float MaxHealth;
    public float Shield;
    public float MaxShield;
    public float Overheal;
    public float MaxOverheal;
    public float ReservedHealth;
    public float BaseMaxHealth;

    public float EffectiveMaxHealth => MaxHealth;
    public float TotalCurrent => CurrentHealth + Shield + Overheal;
    public float TotalCapacity => MaxHealth + MaxShield + MaxOverheal;
    public float HealthNormalized => MaxHealth > 0f ? Mathf.Clamp01(CurrentHealth / MaxHealth) : 0f;
    public float ShieldNormalized => MaxShield > 0f ? Mathf.Clamp01(Shield / MaxShield) : 0f;
    public float OverhealNormalized => MaxOverheal > 0f ? Mathf.Clamp01(Overheal / MaxOverheal) : 0f;
    public float ReservedNormalized => BaseMaxHealth > 0f ? Mathf.Clamp01(ReservedHealth / BaseMaxHealth) : 0f;
}

public class PlayerHealth : MonoBehaviour, IDamagable
{
    public static event Action<float, float, bool> OnHealthChanged;
    public static event Action<PlayerHealthState, bool> OnHealthStateChanged;

    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _shield;
    [SerializeField] private float _maxShield;
    [SerializeField] private float _overheal;
    [SerializeField] private float _maxOverheal;
    [SerializeField] private float _reservedHealth;
    [SerializeField] private PlayerInventory inventory;

    [Header("Shield Regeneration Settings")]
    [SerializeField] private float shieldRegenDelay = 2f;
    [SerializeField] private float baseShieldRegenRate = 10f;
    [SerializeField] private float shieldRegenExponent = 1.5f;

    private float _lastDamageTime = -Mathf.Infinity;
    private float _invincibleUntilTime = -Mathf.Infinity;
    private float _baseMaxHealth;
    private readonly List<ItemHealthGrant> _itemHealthGrants = new List<ItemHealthGrant>();
    private bool _initialized;
    private bool _isDead;
    private bool _subscribedToInventory;

    public bool IsInvincible => Time.time < _invincibleUntilTime;
    public bool IsDead => _isDead;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float NormalizedHealth => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
    public float Shield => _shield;
    public float MaxShield => _maxShield;
    public float Overheal => _overheal;
    public float MaxOverheal => _maxOverheal;
    public float ReservedHealth => _reservedHealth;
    public PlayerHealthState CurrentState => BuildState();

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
        if (_isDead)
        {
            return;
        }

        if (Time.time - _lastDamageTime >= shieldRegenDelay && _shield < _maxShield)
        {
            ApplyShieldRegeneration();
        }
    }

    public void Initialize(float maxHealth)
    {
        _baseMaxHealth = maxHealth;
        RecalculateHealthLayers(false);
        _currentHealth = _maxHealth;
        _shield = _maxShield;
        _overheal = Mathf.Min(_overheal, _maxOverheal);
        _initialized = true;
        _isDead = false;
        _lastDamageTime = Time.time;
        PublishHealthChanged(false);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsInvincible || _isDead)
        {
            return;
        }

        float remainingDamage = damage;
        remainingDamage = ConsumeLayer(ref _shield, remainingDamage);
        remainingDamage = ConsumeLayer(ref _overheal, remainingDamage);
        _currentHealth -= remainingDamage;
        _lastDamageTime = Time.time; // Reset the healing delay timer
        if (inventory != null && inventory.ItemContext != null)
        {
            inventory.ItemContext.PublishEvent(new ItemEvent
            {
                Type = ItemEventType.PlayerDamaged,
                Owner = inventory.gameObject,
                Damage = damage,
                ProcCoefficient = 1f
            });
        }
        PublishHealthChanged(false);

        if (_currentHealth <= 0)
        {
            _isDead = true;
            if (inventory == null || inventory.ItemContext == null || !inventory.ItemContext.TryHandlePlayerDeath(this))
            {
                Die();
            }
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
        if (_isDead)
        {
            return;
        }

        float percentageHeal = _maxHealth * percentageAmount / 100f;
        float totalHealAmount = rawAmount + percentageHeal;
        if (totalHealAmount <= 0f)
        {
            return;
        }

        _currentHealth = Mathf.Min(_currentHealth + totalHealAmount, _maxHealth);
        PublishHealthChanged(true);
    }

    public void AddShield(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _shield = Mathf.Min(_shield + amount, _maxShield);
        PublishHealthChanged(true);
    }

    public void AddOverheal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _overheal = Mathf.Min(_overheal + amount, _maxOverheal);
        PublishHealthChanged(true);
    }

    public void SetItemHealthGrant(
        ItemRuntime sourceItem,
        float bonusMaxHealth = 0f,
        float bonusMaxShield = 0f,
        float bonusMaxShieldPercentOfMaxHealth = 0f,
        float bonusMaxOverheal = 0f,
        float reservedHealthPercent = 0f)
    {
        if (sourceItem == null)
        {
            return;
        }

        ItemHealthGrant grant = GetOrCreateGrant(sourceItem);
        float oldMaxHealth = _maxHealth;
        float oldMaxShield = _maxShield;
        float oldMaxOverheal = _maxOverheal;

        grant.BonusMaxHealth = Mathf.Max(0f, bonusMaxHealth);
        grant.BonusMaxShield = Mathf.Max(0f, bonusMaxShield);
        grant.BonusMaxShieldPercentOfMaxHealth = Mathf.Max(0f, bonusMaxShieldPercentOfMaxHealth);
        grant.BonusMaxOverheal = Mathf.Max(0f, bonusMaxOverheal);
        grant.ReservedHealthPercent = Mathf.Clamp01(reservedHealthPercent);

        RecalculateHealthLayers(false);
        FillNewGrantCapacity(oldMaxHealth, oldMaxShield, oldMaxOverheal);
        PublishHealthChanged(true);
    }

    public void RemoveItemHealthGrant(ItemRuntime sourceItem)
    {
        if (sourceItem == null)
        {
            return;
        }

        for (int i = _itemHealthGrants.Count - 1; i >= 0; i--)
        {
            if (_itemHealthGrants[i].SourceItem == sourceItem)
            {
                _itemHealthGrants.RemoveAt(i);
            }
        }

        RecalculateHealthLayers(false);
        PublishHealthChanged(true);
    }

    public void RemoveAllItemHealthGrants()
    {
        _itemHealthGrants.Clear();
        RecalculateHealthLayers(false);
        PublishHealthChanged(true);
    }

    public void AddMaxHealthModifier(float amount)
    {
        _baseMaxHealth += amount;
        RecalculateHealthLayers(false);
        PublishHealthChanged(true);
    }

    private void ApplyShieldRegeneration()
    {
        if (_maxShield <= 0f)
        {
            return;
        }

        float timeSinceDamage = Time.time - _lastDamageTime - shieldRegenDelay;
        float healingMultiplier = 1f + Mathf.Pow(timeSinceDamage, shieldRegenExponent);
        
        float shieldThisFrame = baseShieldRegenRate * healingMultiplier * Time.deltaTime;
        _shield = Mathf.Min(_shield + shieldThisFrame, _maxShield);
        PublishHealthChanged(true);
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log("Player died!");
        // Add death animation, game over screen logic, or respawn here
        // gameObject.SetActive(false);
    }

    private void HandleInventoryUpdated(ItemRuntime item)
    {
        if (item != null && item.StackSize <= 0)
        {
            RemoveItemHealthGrant(item);
            return;
        }

        RecalculateHealthLayers(true);
        PublishHealthChanged(true);
    }

    private void RecalculateHealthLayers(bool healIncrease)
    {
        if (!_initialized)
        {
            _maxHealth = GetCalculatedMaxHealth();
            _maxShield = GetGrantedMaxShield(_maxHealth);
            _maxOverheal = GetGrantedMaxOverheal();
            _reservedHealth = GetReservedHealth(_maxHealth);
            _maxHealth = Mathf.Max(1f, _maxHealth - _reservedHealth);
            return;
        }

        float oldMaxHealth = _maxHealth;
        _maxHealth = GetCalculatedMaxHealth();
        _maxShield = GetGrantedMaxShield(_maxHealth);
        _maxOverheal = GetGrantedMaxOverheal();
        _reservedHealth = GetReservedHealth(_maxHealth);
        _maxHealth = Mathf.Max(1f, _maxHealth - _reservedHealth);

        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }

        float gainedHealth = _maxHealth - oldMaxHealth;
        if (healIncrease && gainedHealth > 0f)
        {
            _currentHealth = Mathf.Min(_currentHealth + gainedHealth, _maxHealth);
        }

        _shield = Mathf.Min(_shield, _maxShield);
        _overheal = Mathf.Min(_overheal, _maxOverheal);
    }

    public void Revive(float normalizedHealth, float invincibilityDuration = 0f)
    {
        float targetHealth = _maxHealth * Mathf.Clamp01(normalizedHealth);
        _currentHealth = Mathf.Clamp(targetHealth, 1f, _maxHealth);
        _isDead = false;
        _lastDamageTime = Time.time;
        GrantInvincibility(invincibilityDuration);
        PublishHealthChanged(true);
    }

    private float GetCalculatedMaxHealth()
    {
        float calculatedMaxHealth = inventory != null && inventory.ItemContext != null
            ? inventory.ItemContext.CalculatePlayerStat(PlayerStatType.MaxHealth, _baseMaxHealth)
            : _baseMaxHealth;

        return calculatedMaxHealth + GetGrantedMaxHealth();
    }

    private float GetGrantedMaxHealth()
    {
        float value = 0f;
        for (int i = 0; i < _itemHealthGrants.Count; i++)
        {
            value += _itemHealthGrants[i].BonusMaxHealth;
        }

        return value;
    }

    private float GetGrantedMaxShield(float maxHealthBeforeReserve)
    {
        float value = 0f;
        for (int i = 0; i < _itemHealthGrants.Count; i++)
        {
            value += _itemHealthGrants[i].BonusMaxShield;
            value += maxHealthBeforeReserve * _itemHealthGrants[i].BonusMaxShieldPercentOfMaxHealth;
        }

        return value;
    }

    private float GetGrantedMaxOverheal()
    {
        float value = 0f;
        for (int i = 0; i < _itemHealthGrants.Count; i++)
        {
            value += _itemHealthGrants[i].BonusMaxOverheal;
        }

        return value;
    }

    private float GetReservedHealth(float maxHealthBeforeReserve)
    {
        float reservedPercent = 0f;
        for (int i = 0; i < _itemHealthGrants.Count; i++)
        {
            reservedPercent += _itemHealthGrants[i].ReservedHealthPercent;
        }

        return maxHealthBeforeReserve * Mathf.Clamp01(reservedPercent);
    }

    private ItemHealthGrant GetOrCreateGrant(ItemRuntime sourceItem)
    {
        for (int i = 0; i < _itemHealthGrants.Count; i++)
        {
            if (_itemHealthGrants[i].SourceItem == sourceItem)
            {
                return _itemHealthGrants[i];
            }
        }

        ItemHealthGrant grant = new ItemHealthGrant { SourceItem = sourceItem };
        _itemHealthGrants.Add(grant);
        return grant;
    }

    private void FillNewGrantCapacity(float oldMaxHealth, float oldMaxShield, float oldMaxOverheal)
    {
        float gainedHealth = _maxHealth - oldMaxHealth;
        if (gainedHealth > 0f)
        {
            _currentHealth = Mathf.Min(_currentHealth + gainedHealth, _maxHealth);
        }

        float gainedShield = _maxShield - oldMaxShield;
        if (gainedShield > 0f)
        {
            _shield = Mathf.Min(_shield + gainedShield, _maxShield);
        }

        float gainedOverheal = _maxOverheal - oldMaxOverheal;
        if (gainedOverheal > 0f)
        {
            _overheal = Mathf.Min(_overheal + gainedOverheal, _maxOverheal);
        }
    }

    private PlayerHealthState BuildState()
    {
        return new PlayerHealthState
        {
            CurrentHealth = _currentHealth,
            MaxHealth = _maxHealth,
            Shield = _shield,
            MaxShield = _maxShield,
            Overheal = _overheal,
            MaxOverheal = _maxOverheal,
            ReservedHealth = _reservedHealth,
            BaseMaxHealth = Mathf.Max(1f, GetCalculatedMaxHealth())
        };
    }

    private void PublishHealthChanged(bool isHealing)
    {
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, isHealing);
        OnHealthStateChanged?.Invoke(BuildState(), isHealing);
    }

    private static float ConsumeLayer(ref float layerValue, float damage)
    {
        if (damage <= 0f || layerValue <= 0f)
        {
            return damage;
        }

        float absorbed = Mathf.Min(layerValue, damage);
        layerValue -= absorbed;
        return damage - absorbed;
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

    private class ItemHealthGrant
    {
        public ItemRuntime SourceItem;
        public float BonusMaxHealth;
        public float BonusMaxShield;
        public float BonusMaxShieldPercentOfMaxHealth;
        public float BonusMaxOverheal;
        public float ReservedHealthPercent;
    }
}
