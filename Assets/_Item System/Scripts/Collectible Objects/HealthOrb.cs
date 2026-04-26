using System;
using UnityEngine;

public class HealthOrb : Collectible, ICollectible
{
    public float healAmount = 20f;
    public static event Action<float> OnHealthOrbCollected;

    // This is the method the CollectibleCollector calls
    public override void OnCollected()
    {
        // Add a debug log to verify it's working in the console
        Debug.Log("Health Orb collected!");

        // Trigger the gameplay logic
        OnHealthOrbCollected?.Invoke(healAmount);

        // Deactivate the orb. 
        // This will trigger OnDisable in the base class, 
        // which automatically unregisters the orb from the ItemCollectibleManager.
        gameObject.SetActive(false); 
    }

    // You can keep this for compatibility with other systems using ICollectible
    public void Collect(GameObject collector)
    {
        OnCollected();
    }
}