using System;
using UnityEngine;

public class HealthOrb : Collectible
{
    public float healAmount = 20f;
    public static event Action<float> OnHealthOrbCollected;

    // This is the method the CollectibleCollector calls
    public override void OnCollected()
    {
        // Add a debug log to verify it's working in the console
        Debug.Log("Health Orb collected!");

        EconomyManager.Instance.AddMoney((int)healAmount); // Add currency to the player

         // Trigger the healing effect on the player
        // Trigger the gameplay logic
        OnHealthOrbCollected?.Invoke(healAmount);

        // Deactivate the orb. 
        // This will trigger OnDisable in the base class, 
        // which automatically unregisters the orb from the ItemCollectibleManager.
        gameObject.SetActive(false); 
    }


}