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
        public int creditCost; // The "Price" for the Economy Track
    }

    [Header("Economy & Leveling")]
    [SerializeField] private int initialCredits = 200;
    [SerializeField] private float baseCreditsPerSecond = 5f;
    [SerializeField] private float creditsPerLevelMultiplier = 1.25f;
    [SerializeField] private float timeDifficultyScaling = 0.05f; // Extra scaling per minute

    [Header("Dynamic Enemy Cap")]
    [Tooltip("The base number of enemies allowed at Level 1")]
    [SerializeField] private float baseMaxEnemies = 15f;
    [Tooltip("How many more slots open up per Level (Float)")]
    [SerializeField] private float extraEnemiesPerLevel = 2.5f;
    [Tooltip("The absolute limit to protect your GPU/CPU")]
    [SerializeField] private int hardMaxEnemies = 50;

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

    // Internal State
    private float _currentCredits;
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

        // Initialize Cap and Credits
        UpdateWorldLevel(1); 
        _currentCredits = initialCredits;
        
        // Phase 1: Pre-populate the map bounds
        PopulateMapAtStart();
    }

    void OnDisable() => GameManager.OnLevelChanged -= UpdateWorldLevel;

    void Update()
    {
        _gameTimer += Time.deltaTime;

        // 1. Economy Calculation
        float scalingFactor = 1f + (_gameTimer / 60f * timeDifficultyScaling);
        float income = baseCreditsPerSecond * Mathf.Pow(creditsPerLevelMultiplier, _worldLevel - 1) * scalingFactor;
        _currentCredits += income * Time.deltaTime;

        // Stop all spawning if at the cap
        if (_activeEnemies.Count >= _currentMaxEnemies) return;

        // 2. Track A: Credit-based Donut Spawning
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

    private bool TryCreditSpawn()
    {
        // Pick random enemy
        EnemyDefinition selection = enemyPool[Random.Range(0, enemyPool.Count)];

        // If credits are high and slots are low, the Director eventually 
        // "must" pick the expensive ones to spend its cash.
        if (_currentCredits >= selection.creditCost)
        {
            Vector2 pos = GetDonutPosition();
            if (IsValidSpawnPoint(pos, false))
            {
                if (SpawnFromPool(selection, pos))
                {
                    _currentCredits -= selection.creditCost;
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
        while (_currentCredits >= 10 && _activeEnemies.Count < _currentMaxEnemies && safety < 100)
        {
            EnemyDefinition selection = enemyPool[Random.Range(0, enemyPool.Count)];
            if (_currentCredits >= selection.creditCost)
            {
                Vector2 pos = new Vector2(Random.Range(mapBoundsMin.x, mapBoundsMax.x), Random.Range(mapBoundsMin.y, mapBoundsMax.y));
                if (IsValidSpawnPoint(pos, false))
                {
                    if (SpawnFromPool(selection, pos)) _currentCredits -= selection.creditCost;
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
        
        // Set the enemy's strength based on the global level
        obj.GetComponent<BaseEnemy>()?.SetLevel(_worldLevel);

        obj.SetActive(true);
        _activeEnemies.Add(obj);
        return true;
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