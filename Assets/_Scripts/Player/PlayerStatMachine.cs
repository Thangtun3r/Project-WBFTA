using UnityEngine;
using _Scripts;

public class PlayerStatMachine : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private PlayerInventory inventory;

    [Header("Base Stats (Set these)")]
    [SerializeField] private float baseCritRate = 0.2f; // Renamed to "base" for clarity
    [SerializeField] private float baseCritDamage = 1.5f; 
    
    [Header("Live Stats (Read Only - Watch these update!)")]
    [SerializeField] private float currentCritRateDisplay;
    [SerializeField] private float currentDamageDisplay;

    private float _baseDamage;
    private float _baseHealth;
    private bool _wasLastAttackCrit;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogWarning("PlayerStatMachine: No PlayerConfig assigned!");
            return;
        }

        _baseDamage = config.damage;
        _baseHealth = config.maxHealth;
        
        // Initialize our displays
        RefreshInspectorStats(null); 
    }

    // --- EVENT LISTENER SETUP ---
    private void OnEnable()
    {
        // Listen to the inventory. Whenever it changes, update our Inspector!
        if (inventory != null)
        {
            inventory.InventoryUpdated += RefreshInspectorStats;
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe when destroyed to prevent memory leaks
        if (inventory != null)
        {
            inventory.InventoryUpdated -= RefreshInspectorStats;
        }
    }

    /// <summary>
    /// This runs automatically every time an item is added, removed, or stacked
    /// </summary>
    private void RefreshInspectorStats(ItemRuntime item)
    {
        currentCritRateDisplay = GetCalculatedCritRate();
        currentDamageDisplay = _baseDamage; // Later, change this to GetCalculatedBaseDamage() if you add damage items!
    }
    // ----------------------------

    /// <summary>
    /// Calculates the actual damage output including crit chance and crit damage
    /// </summary>
    public float GetCalculatedAttackDamage()
{
    float finalDamage = _baseDamage;
    _wasLastAttackCrit = false;

    // 1. Get the actual crit rate 
    float currentCritRate = GetCalculatedCritRate();

    if (Random.value <= currentCritRate)
    {
        // 2. Get the actual crit damage multiplier that includes items!
        float currentCritDamage = GetCalculatedCritDamage();

        // 3. Multiply by the calculated value, NOT the base value
        finalDamage *= currentCritDamage;
        
        _wasLastAttackCrit = true;
        Debug.Log($"CRIT! Damage: {finalDamage}");
    }

    return finalDamage;
}

    public bool WasLastAttackCrit() => _wasLastAttackCrit;
    
    public float GetBaseDamage() => _baseDamage;
    
    public float GetBaseHealth() => _baseHealth;

    public float GetCalculatedCritDamage()
    {
        return baseCritDamage; 
    }
    
    public float GetCalculatedCritRate()
    {
        return Mathf.Clamp01(baseCritRate);
    }
    
    // --- Modifier Methods called by Items ---
    
    public void AddCritRateModifier(float amount)
    {
        baseCritRate += amount;
        RefreshInspectorStats(null);
    }

    public void AddCritDamageModifier(float amount)
    {
        baseCritDamage += amount;
        RefreshInspectorStats(null);
    }



    public void SetCritRate(float newCritRate)
    {
        baseCritRate = Mathf.Clamp01(newCritRate);
        RefreshInspectorStats(null); // Update UI if we change this via code
    }

    public void SetCritDamage(float newCritDamage)
    {
        baseCritDamage = Mathf.Max(1f, newCritDamage);
    }
}