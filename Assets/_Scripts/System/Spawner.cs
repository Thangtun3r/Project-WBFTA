using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.Enemy;

public class DirectorSpawner2D : MonoBehaviour
{
    [System.Serializable]
    public class EnemyDefinition
    {
        public string enemyName;
        public GameObject prefab;
        [Range(0f, 100f)]
        public float weight; // Weight cost for spawning this enemy (use slider)
    }

    [Header("Weight & Leveling")]
    [Range(0f, 100f)]
    [SerializeField] private float currentWeight = 50f; // Slider for current weight
    [SerializeField] private float maxWeight = 100f;

    [Header("Dynamic Enemy Cap")]
    [Tooltip("The base number of enemies allowed at Level 1")]
    [SerializeField] private float baseMaxEnemies = 15f;
    [Tooltip("How many more slots open up per Level (Float)")]
    [SerializeField] private float extraEnemiesPerLevel = 2.5f;
    [Tooltip("The absolute limit to protect your GPU/CPU")]
    [SerializeField] private int hardMaxEnemies = 50;
    [Tooltip("Spawn more enemies when count drops below this threshold")]
    [SerializeField] private float minEnemyThreshold = 0.75f; // 75% of max

    [Header("Track A: Economy Maintenance (Off-Screen)")]
    [SerializeField] private float spawnRadiusMin = 15f; 
    [SerializeField] private float spawnRadiusMax = 22f;
    [SerializeField] private float creditSpawnCooldown = 1.5f;

    [Header("Track B: RNG Maintenance (Viewport Spawns)")]
    [Range(0f, 100f)] [SerializeField] private float viewportSpawnChance = 10f; 
    [SerializeField] private float rngCheckInterval = 4.0f;
    [SerializeField] private float viewportSafeZone = 4f; // Distance from player center

    [Header("Map & Safety")]
    [SerializeField] private Vector2 mapBoundsMin;
    [SerializeField] private Vector2 mapBoundsMax;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private List<EnemyDefinition> enemyPool = new List<EnemyDefinition>();

    [Header("Spawn Loading Indicator")]
    [SerializeField] private GameObject loadingIndicatorPrefab;
    [SerializeField] private float spawnLoadingDelay = 1.5f;

    // Internal State
    private float _gameTimer;
    private int _worldLevel = 1;
    private int _currentMaxEnemies;
    private float _nextCreditSpawnTime;
    private float _nextRngCheckTime;

    private HashSet<GameObject> _activeEnemies = new HashSet<GameObject>();
    private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    void Start()
    {
        InitializePools();
        GameManager.OnLevelChanged += UpdateWorldLevel;

        // Initialize Cap
        UpdateWorldLevel(1); 
        
        // Phase 1: Pre-populate the map bounds
        PopulateMapAtStart();
    }

    void OnDisable() => GameManager.OnLevelChanged -= UpdateWorldLevel;

    void Update()
    {
        _gameTimer += Time.deltaTime;

        // Clamp weight to max value
        currentWeight = Mathf.Min(currentWeight, maxWeight);

        // Count currently enabled enemies
        int enabledEnemyCount = CountEnabledEnemies();
        
        // Calculate threshold for spawning
        int spawnThreshold = Mathf.CeilToInt(_currentMaxEnemies * minEnemyThreshold);

        // Only spawn if enabled enemy count drops below threshold
        if (enabledEnemyCount < spawnThreshold)
        {
            // 2. Track A: Weight-based Donut Spawning
            if (Time.time >= _nextCreditSpawnTime)
            {
                if (TryCreditSpawn()) _nextCreditSpawnTime = Time.time + creditSpawnCooldown;
            }

            // 3. Track B: Chance-based Viewport Spawning
            if (Time.time >= _nextRngCheckTime)
            {
                TryRNGViewportSpawn();
                _nextRngCheckTime = Time.time + rngCheckInterval;
            }
        }
    }

    private int CountEnabledEnemies()
    {
        int count = 0;
        foreach (var enemy in _activeEnemies)
        {
            if (enemy != null && enemy.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    private bool TryCreditSpawn()
    {
        // Pick random enemy
        EnemyDefinition selection = enemyPool[Random.Range(0, enemyPool.Count)];

        // If weight is high enough, spawn the enemy
        if (currentWeight >= selection.weight)
        {
            Vector2 pos = GetDonutPosition();
            if (IsValidSpawnPoint(pos, false))
            {
                if (SpawnFromPool(selection, pos))
                {
                    currentWeight -= selection.weight;
                    return true;
                }
            }
        }
        return false;
    }

    private void TryRNGViewportSpawn()
    {
        if (Random.Range(0f, 100f) <= viewportSpawnChance)
        {
            EnemyDefinition selection = enemyPool[Random.Range(0, enemyPool.Count)];
            Vector2 pos = GetViewportPosition();

            if (IsValidSpawnPoint(pos, true))
            {
                SpawnFromPool(selection, pos);
                // Viewport spawns are FREE (Track B)
            }
        }
    }

    // --- Validation & Placement ---

    private bool IsValidSpawnPoint(Vector2 pos, bool isViewport)
    {
        // Wall check
        if (Physics2D.OverlapCircle(pos, 0.5f, obstacleLayer)) return false;
        
        // Distance check (using specific limits for donut vs viewport)
        float dist = Vector2.Distance(pos, Camera.main.transform.position);
        float minAllowed = isViewport ? viewportSafeZone : spawnRadiusMin;

        return dist >= minAllowed;
    }

    private Vector2 GetDonutPosition()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(spawnRadiusMin, spawnRadiusMax);
        return (Vector2)Camera.main.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    private Vector2 GetViewportPosition()
    {
        // 0.2 to 0.8 ensures they aren't right on the screen edge
        float x = Random.Range(0.2f, 0.8f);
        float y = Random.Range(0.2f, 0.8f);
        return (Vector2)Camera.main.ViewportToWorldPoint(new Vector3(x, y, 0));
    }

    private void PopulateMapAtStart()
    {
        int safety = 0;
        while (currentWeight >= 5f && _activeEnemies.Count < _currentMaxEnemies && safety < 100)
        {
            EnemyDefinition selection = enemyPool[Random.Range(0, enemyPool.Count)];
            if (currentWeight >= selection.weight)
            {
                Vector2 pos = new Vector2(Random.Range(mapBoundsMin.x, mapBoundsMax.x), Random.Range(mapBoundsMin.y, mapBoundsMax.y));
                if (IsValidSpawnPoint(pos, false))
                {
                    if (SpawnFromPool(selection, pos)) currentWeight -= selection.weight;
                }
            }
            safety++;
        }
    }

    // --- System Management ---

    private void UpdateWorldLevel(int level)
    {
        _worldLevel = level;

        // Dynamic Cap Calculation (Float -> Int with hard clamp)
        float dynamicCap = baseMaxEnemies + (level - 1) * extraEnemiesPerLevel;
        _currentMaxEnemies = Mathf.Min(hardMaxEnemies, Mathf.FloorToInt(dynamicCap));

        Debug.Log($"Level {level} Active. Max Enemies: {_currentMaxEnemies}.");
    }

    private bool SpawnFromPool(EnemyDefinition def, Vector2 pos)
    {
        if (!_pools.ContainsKey(def.prefab) || _pools[def.prefab].Count == 0) return false;

        GameObject obj = _pools[def.prefab].Dequeue();
        obj.transform.position = pos;
        
        // Start delayed spawn with loading indicator
        StartCoroutine(DelayedSpawn(obj, def, pos));
        _activeEnemies.Add(obj);
        return true;
    }

    private IEnumerator DelayedSpawn(GameObject enemyObj, EnemyDefinition def, Vector2 pos)
    {
        // Spawn loading indicator at position
        GameObject loadingIndicator = null;
        if (loadingIndicatorPrefab != null)
        {
            loadingIndicator = Instantiate(loadingIndicatorPrefab, pos, Quaternion.identity);
        }

        // Wait for the spawn delay
        yield return new WaitForSeconds(spawnLoadingDelay);

        // Destroy loading indicator
        if (loadingIndicator != null)
        {
            Destroy(loadingIndicator);
        }

        // Set the enemy's strength based on the global level
        enemyObj.GetComponent<BaseEnemy>()?.SetLevel(_worldLevel);

        // Finally activate the enemy
        enemyObj.SetActive(true);
    }

    public void OnEnemyDeath(GameObject obj, GameObject prefabKey)
    {
        _activeEnemies.Remove(obj);
        _pools[prefabKey].Enqueue(obj);
    }

    private void InitializePools()
    {
        foreach (var def in enemyPool)
        {
            Queue<GameObject> queue = new Queue<GameObject>();
            for (int i = 0; i < 25; i++)
            {
                GameObject obj = Instantiate(def.prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
            _pools.Add(def.prefab, queue);
        }
    }

    // --- Gizmos ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        DrawRectGizmo(mapBoundsMin, mapBoundsMax);

        if (Application.isPlaying && Camera.main != null)
        {
            Gizmos.color = Color.red;
            DrawCircleGizmo(Camera.main.transform.position, spawnRadiusMin, 32);
            Gizmos.color = Color.yellow;
            DrawCircleGizmo(Camera.main.transform.position, viewportSafeZone, 16);
        }
    }

    private void DrawRectGizmo(Vector2 min, Vector2 max)
    {
        Vector3 tl = new Vector3(min.x, max.y, 0);
        Vector3 tr = new Vector3(max.x, max.y, 0);
        Vector3 br = new Vector3(max.x, min.y, 0);
        Vector3 bl = new Vector3(min.x, min.y, 0);
        Gizmos.DrawLine(tl, tr); Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl); Gizmos.DrawLine(bl, tl);
    }

    private void DrawCircleGizmo(Vector3 center, float radius, int segments)
    {
        float step = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * step * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}