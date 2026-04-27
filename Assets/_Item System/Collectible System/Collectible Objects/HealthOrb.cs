using System;
using UnityEngine;

public class HealthOrb : Collectible
{
    public float healAmount = 20f;
    public static event Action<float> OnHealthOrbCollected;

  
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