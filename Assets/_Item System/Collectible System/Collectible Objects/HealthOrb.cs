using System;
using UnityEngine;

public class HealthOrb : Collectible, IWorldObjectSpawner // Added the interface
{
    public float healAmount;

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
            playerHealth.Heal(0f, healAmount);
        }

        // todo: maybe use object pooling for this later on
        Destroy(gameObject);
    }
}