using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    public static event Action<float, float, bool> OnHealthChanged;

    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;

    [Header("Regeneration Settings")]
    [SerializeField] private float healingDelay = 2f; // Wait this long after damage before healing starts
    [SerializeField] private float baseHealingRate = 10f; // Base health per second
    [SerializeField] private float healingExponent = 1.5f; // Exponential growth factor (higher = faster acceleration)

    private float _lastDamageTime = -Mathf.Infinity;


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
        _maxHealth = maxHealth;
        _currentHealth = _maxHealth;
        _lastDamageTime = Time.time;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, false);
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _lastDamageTime = Time.time; // Reset the healing delay timer
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, false);

        if (_currentHealth <= 0)
        {
            Die();
        }
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
        _maxHealth += amount;
        
        // Prevent current health from exceeding the new max health if max health was reduced
        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }

        // Notify UI/systems that max health has changed (true = beneficial change, no damage vignette)
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, true);
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
}