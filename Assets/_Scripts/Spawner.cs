using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner2D : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnWeight
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float weight;
    }

    [Header("Spawn Settings")]
    [SerializeField] private List<EnemySpawnWeight> enemyPrefabs = new List<EnemySpawnWeight>();
    
    [Header("Pool Settings")]
    [SerializeField] private int poolSizePerPrefab = 15;

    [Header("Spawn Logic")]
    [SerializeField] private Collider2D spawnArea; // Use Collider2D (Box, Polygon, or Composite)
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 2f;
    [SerializeField] private int maxActiveEnemiesOverall = 30;
    [Tooltip("Maximum number of enemies allowed to be visible on screen before spawning stops")]
    [SerializeField] private int maxVisibleEnemies = 5;

    // Dictionary tracking the pool for each specific prefab
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    
    // Hashset for fast O(1) removal, used to track what is currently alive on screen
    private HashSet<GameObject> activeEnemies = new HashSet<GameObject>();
    
    private float timer = 0f;
    private float currentSpawnInterval;

    void Start()
    {
        InitializePools();
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void InitializePools()
    {
        foreach (var spawnWeight in enemyPrefabs)
        {
            if (spawnWeight.prefab == null) continue;
            if (pools.ContainsKey(spawnWeight.prefab)) continue;

            Queue<GameObject> newPool = new Queue<GameObject>();
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject obj = Instantiate(spawnWeight.prefab, transform);
                obj.SetActive(false);
                newPool.Enqueue(obj);
            }
            pools.Add(spawnWeight.prefab, newPool);
        }
    }

    private GameObject GetRandomWeightedPrefab()
    {
        if (enemyPrefabs.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var ew in enemyPrefabs) totalWeight += ew.weight;

        float randomVal = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var ew in enemyPrefabs)
        {
            currentWeight += ew.weight;
            if (randomVal <= currentWeight)
            {
                return ew.prefab;
            }
        }

        return enemyPrefabs[0].prefab; // Fallback
    }

    private int CountEnemiesInViewport()
    {
        Camera cam = Camera.main;
        if (cam == null) return 0;

        int count = 0;
        foreach (var enemy in activeEnemies)
        {
            if (!enemy.activeInHierarchy) continue; // Skip inactive enemies
            
            Vector3 vp = cam.WorldToViewportPoint(enemy.transform.position);
            // Check if coordinates are within the camera frustum limits
            if (vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0)
            {
                count++;
            }
        }
        return count;
    }

    private Vector2 GetPointWithinCameraViewport()
    {
        Camera cam = Camera.main;
        if (cam == null) return (Vector2)transform.position;

        float zDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 viewportPoint = new Vector3(Random.value, Random.value, zDistance);
        Vector3 worldPoint = cam.ViewportToWorldPoint(viewportPoint);
        Vector2 spawnPos = new Vector2(worldPoint.x, worldPoint.y);

        if (spawnArea != null)
        {
            spawnPos = spawnArea.ClosestPoint(spawnPos);
        }

        return spawnPos;
    }

    public GameObject Spawn(Vector2 position, Quaternion rotation)
    {
        GameObject prefabToSpawn = GetRandomWeightedPrefab();
        if (prefabToSpawn == null || !pools.ContainsKey(prefabToSpawn)) return null;

        Queue<GameObject> pool = pools[prefabToSpawn];
        
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            
            activeEnemies.Add(obj);
            // Removed auto-despawn. Enemies only return to pool when explicitly called (killed/removed).
            return obj;
        }
        return null;
    }

    public void ReturnToPool(GameObject obj, GameObject prefabKey)
    {
        if (!obj.activeInHierarchy) return;
        
        obj.SetActive(false);
        activeEnemies.Remove(obj);
        
        if (pools.ContainsKey(prefabKey))
        {
            pools[prefabKey].Enqueue(obj);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= currentSpawnInterval)
        {
            // Only spawn if we haven't hit the hard limit AND the screen isn't already cluttered
            if (activeEnemies.Count < maxActiveEnemiesOverall && CountEnemiesInViewport() < maxVisibleEnemies)
            {
                Vector2 spawnPos = GetPointWithinCameraViewport();
                Spawn(spawnPos, Quaternion.identity);

                timer = 0f;
                currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            }
        }
    }
}