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
        Debug.Log($"Player took {damage} damage! Current health: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, true);
        Debug.Log($"Player healed {healAmount}! Current health: {_currentHealth}/{_maxHealth}");
    }

    private void ApplyPassiveHealing()
    {
        // Calculate time since damage (after the delay has passed)
        float timeSinceDamage = Time.time - _lastDamageTime - healingDelay;
        
        // Exponential healing multiplier: 1 + (timeSinceDamage ^ exponent)
        // This makes healing accelerate faster the longer without damage
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