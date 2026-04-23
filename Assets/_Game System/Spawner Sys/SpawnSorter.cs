using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnSorter
{
    public List<EnemySpawnerDatabase.EnemyEntry> CreateManifest(EnemySpawnerDatabase db, float credits, int maxSlots)
    {
        List<EnemySpawnerDatabase.EnemyEntry> manifest = new List<EnemySpawnerDatabase.EnemyEntry>();
        if (db == null || db.allEnemies.Count == 0 || maxSlots <= 0) return manifest;

        // Get the pool of cheapest enemies
        var cheapestPool = db.allEnemies.Where(e => e.cost == db.GetCheapest().cost).ToList();
        float remainingCredits = credits;

        // FIX: Move the random pick INSIDE the for-loop for variety
        int countToSpawn = Mathf.Min(maxSlots, Mathf.FloorToInt(credits / db.GetCheapest().cost));
        for (int i = 0; i < countToSpawn; i++)
        {
            var unitForThisSlot = GetWeightedRandom(cheapestPool); // Pick a new one each time
            manifest.Add(unitForThisSlot);
            remainingCredits -= unitForThisSlot.cost;
        }

        // SWAP: Only triggers if you have enemies with DIFFERENT costs
        bool canStillUpgrade = true;
        while (remainingCredits > 0 && canStillUpgrade)
        {
            canStillUpgrade = false;
            for (int i = 0; i < manifest.Count; i++)
            {
                var current = manifest[i];
                var upgrades = db.allEnemies
                    .Where(e => e.cost > current.cost && (e.cost - current.cost) <= remainingCredits)
                    .ToList();

                if (upgrades.Count > 0)
                {
                    manifest[i] = GetWeightedRandom(upgrades);
                    remainingCredits -= (manifest[i].cost - current.cost);
                    canStillUpgrade = true;
                }
            }
        }
        return manifest;
    }

    private EnemySpawnerDatabase.EnemyEntry GetWeightedRandom(List<EnemySpawnerDatabase.EnemyEntry> pool)
    {
        if (pool.Count == 1) return pool[0];

        float totalWeight = pool.Sum(e => e.weight);
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in pool)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry;
        }

        return pool[0];
    }
}