using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewEnemySpawnerDatabase", menuName = "Spawner/Enemy Spawner Database")]
public class EnemySpawnerDatabase : ScriptableObject
{
    public enum EnemyTier { Fodder, Elite, MiniBoss, Boss }
    
    // NEW: Proximity Preference
    public enum SpawnPreference { NearPlayer, FarFromPlayer, Random }

    [System.Serializable]
    public class EnemyEntry
    {
        public string enemyName;
        public GameObject prefab;
        public float cost;
        [Range(0, 100)] public float weight = 10f; // NEW: Higher = more common
        public EnemyTier tier;
        public SpawnPreference preference;
    }

    public List<EnemyEntry> allEnemies = new List<EnemyEntry>();

    public EnemyEntry GetCheapest() => allEnemies.OrderBy(e => e.cost).FirstOrDefault();
    
    public List<EnemyEntry> GetPotentialUpgrades(float currentCost) => 
        allEnemies.Where(e => e.cost > currentCost).OrderBy(e => e.cost).ToList();

    public void SortDatabase()
    {
        allEnemies = allEnemies.OrderBy(e => e.tier).ThenBy(e => e.cost).ToList();
    }
}