using System;
using UnityEngine;

public class HealthOrb : Collectible, IWorldObjectSpawner // Added the interface
{
    public float healAmount = 20f;
    public static event Action<float> OnHealthOrbCollected;

    // This is where we "Pull the data out"
    public void OnSpawn(object data)
    {
        // We check if the data being handed to us is actually a float
        if (data is float amount)
        {
            healAmount = amount;
        }
    }
  
    public override void OnCollected(GameObject collector)
    {
        PlayerHealth playerHealth = collector.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
            OnHealthOrbCollected?.Invoke(healAmount);
        }

        // todo: maybe use object pooling for this later on
        Destroy(gameObject);
    }
}