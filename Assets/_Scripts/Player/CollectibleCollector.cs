using UnityEngine;
using System.Collections.Generic;

public class CollectibleCollector : MonoBehaviour
{
    public float pickupRadius = 8.57f;
    public float collectionRadius = 0.5f;
    public float moveSpeed = 10f;

    private void Update()
    {
        if (ItemCollectibleManager.Instance == null) return;

        // Optimized query: only checks items in the 9-cell neighborhood
        List<Collectible> nearbyItems = ItemCollectibleManager.Instance.GetNearbyItems(transform.position);
        
        float pSq = pickupRadius * pickupRadius;
        float cSq = collectionRadius * collectionRadius;

        for (int i = 0; i < nearbyItems.Count; i++)
        {
            Collectible item = nearbyItems[i];
            float dSq = (item.transform.position - transform.position).sqrMagnitude;

            if (dSq < pSq)
            {
                // Magnetize towards collector
                item.transform.position = Vector3.MoveTowards(item.transform.position, transform.position, moveSpeed * Time.deltaTime);
                
                // Crucial: Update grid position while moving so manager can still find it
                item.UpdateGridStatus();

                if (dSq < cSq)
                {
                    item.OnCollected();
                }
            }
        }
    }
}