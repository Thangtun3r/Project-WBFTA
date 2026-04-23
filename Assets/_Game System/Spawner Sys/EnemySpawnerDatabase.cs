using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewEnemySpawnerDatabase", menuName = "Spawner/Enemy Spawner Database")]
public class EnemySpawnerDatabase : ScriptableObject
{
    public enum EnemyTier { Fodder, Elite, MiniBoss, Boss }
    
    public enum SpawnPreference 
    { 
        NearPlayer, 
        FarFromPlayer, 
        Random, 
        OnPlayer // Forces spawn exactly at the player's grid position
    }

    [System.Serializable]
    public class EnemyEntry
    {
        public string enemyName;
        public GameObject prefab;
        public float cost;
        [Range(0, 100)] public float weight = 10f;
        public EnemyTier tier;
        public SpawnPreference preference;
    }

    public List<EnemyEntry> allEnemies = new List<EnemyEntry>();

    private EnemyEntry _cheapestCache;

    public EnemyEntry GetCheapest()
    {
        if (allEnemies == null || allEnemies.Count == 0) return null;
        
        if (_cheapestCache == null)
        {
            _cheapestCache = allEnemies.OrderBy(e => e.cost).FirstOrDefault();
        }
        return _cheapestCache;
    }
    
    public List<EnemyEntry> GetPotentialUpgrades(float currentCost)
    {
        return allEnemies
            .Where(e => e.cost > currentCost)
            .OrderBy(e => e.cost)
            .ToList();
    }

    [ContextMenu("Sort Database")]
    public void SortDatabase()
    {
        if (allEnemies == null) return;

        allEnemies = allEnemies
            .OrderBy(e => e.tier)
            .ThenBy(e => e.cost)
            .ThenBy(e => e.enemyName)
            .ToList();
        
        _cheapestCache = null;
    }

    private void OnValidate()
    {
        _cheapestCache = null;
    }
}