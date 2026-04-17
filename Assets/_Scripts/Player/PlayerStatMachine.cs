using UnityEngine;
using _Scripts;

public class PlayerStatMachine : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;
    
    [Header("Crit Stats")]
    [SerializeField] private float critRate = 0.2f; // 20% crit chance
    [SerializeField] private float critDamage = 1.5f; // 1.5x damage on crit
    
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
    }

    /// <summary>
    /// Calculates the actual damage output including crit chance and crit damage
    /// </summary>
    public float GetCalculatedAttackDamage()
    {
        float finalDamage = _baseDamage;
        _wasLastAttackCrit = false;

        // Check if this attack is a crit
        if (Random.value <= critRate)
        {
            finalDamage *= critDamage;
            _wasLastAttackCrit = true;
            Debug.Log($"CRIT! Damage: {finalDamage}");
        }

        return finalDamage;
    }

    /// <summary>
    /// Returns whether the last attack was a critical hit
    /// </summary>
    public bool WasLastAttackCrit()
    {
        return _wasLastAttackCrit;
    }

    /// <summary>
    /// Gets the base damage without crit calculations
    /// </summary>
    public float GetBaseDamage()
    {
        return _baseDamage;
    }

    /// <summary>
    /// Gets the base health
    /// </summary>
    public float GetBaseHealth()
    {
        Debug.Log("PlayerStatMachine: Returning base health: " + _baseHealth);
    
        return _baseHealth;
        
    }

    /// <summary>
    /// Gets the current crit rate (0-1)
    /// </summary>
    public float GetCritRate()
    {
        return critRate;
    }

    /// <summary>
    /// Gets the current crit damage multiplier
    /// </summary>
    public float GetCritDamage()
    {
        return critDamage;
    }

    /// <summary>
    /// Sets new crit rate
    /// </summary>
    public void SetCritRate(float newCritRate)
    {
        critRate = Mathf.Clamp01(newCritRate);
    }

    /// <summary>
    /// Sets new crit damage multiplier
    /// </summary>
    public void SetCritDamage(float newCritDamage)
    {
        critDamage = Mathf.Max(1f, newCritDamage);
    }
}
