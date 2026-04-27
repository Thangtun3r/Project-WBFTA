using UnityEngine;
using System.Collections.Generic;

public class PlayerCollectibleCollector : MonoBehaviour
{
    public float pickupRadius = 8.57f;
    public float collectionRadius = 0.5f;
    public float moveSpeed = 10f;

    private void Update()
    {
        if (ItemCollectibleManager.Instance == null) return;

        // Optimized query: only checks items in the 9-cell neighborhood
        List<Collectible> nearbyItems = ItemCollectibleManager.Instance.GetNearbyItems(transform.position);
        
        float pickupRadiusSquared = pickupRadius * pickupRadius;
        float collectionRadiusSquared = collectionRadius * collectionRadius;

        for (int itemIndex = 0; itemIndex < nearbyItems.Count; itemIndex++)
        {
            Collectible item = nearbyItems[itemIndex];
            float distanceSquared = (item.transform.position - transform.position).sqrMagnitude;

            if (distanceSquared < pickupRadiusSquared)
            {
                // Magnetize item towards collector
                item.transform.position = Vector3.MoveTowards(item.transform.position, transform.position, moveSpeed * Time.deltaTime);
                
                // Update grid position while moving so the manager can still track it
                item.UpdateGridStatus();

                if (distanceSquared < collectionRadiusSquared)
                {
                    item.OnCollected(gameObject);
                }
            }
        }
    }
}