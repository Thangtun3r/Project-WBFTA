using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.Enemy; // Ensure this matches your BaseEnemy namespace

public class DirectorSpawner2D : MonoBehaviour
{
    [Header("Data & Logic")]
    public EnemySpawnerDatabase database;
    private SpawnSorter _sorter;

    [Header("Economy (Exponential)")]
    public float initialCredits = 25f;
    public float baseCreditsPerSecond = 1.5f;
    [Tooltip("Multiplier per level (e.g., 1.12 = 12% increase)")]
    public float incomeGrowthMultiplier = 1.12f; 
    public float spawnInterval = 3f;
    [Tooltip("Director waits until it has this many credits to dump a wave.")]
    public float spawnThreshold = 10f;

    [Header("Capacity & Spawning")]
    public int baseMaxCap = 20;
    public int hardMaxCap = 100;
    public float capGrowthMultiplier = 1.2f;
    public float initialGracePeriod = 10f;
    public GameObject loadingPrefab;
    public float loadingDuration = 1.2f;

    [Header("References")]
    public GridManager gridManager;
    public Transform playerTransform;
    public Transform enemyParent;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private float _currentCredits;
    [SerializeField] private int _currentMaxCap;
    [SerializeField] private float _activeCreditsPerSec;
    [SerializeField] private int _pendingSpawnCount = 0;
    [SerializeField] private List<GameObject> _activeEnemies = new List<GameObject>();

    private float _gameStartTime;

    private void Awake() => _sorter = new SpawnSorter();

    private void OnEnable()
    {
        // Listen to GameManager for level ups
        GameManager.OnLevelChanged += UpdateDifficultyStats;
    }

    private void OnDisable()
    {
        // Cleanup subscription
        GameManager.OnLevelChanged -= UpdateDifficultyStats;
    }

    private void Start()
    {
        _gameStartTime = Time.time;
        _currentCredits = initialCredits;
        
        // Default parent to this object if not assigned
        if (enemyParent == null) enemyParent = this.transform;

        // Sync with GameManager's initial level
        int startLevel = (GameManager.Instance != null) ? GameManager.Instance.CurrentLevel : 1;
        UpdateDifficultyStats(startLevel);

        InvokeRepeating(nameof(ExecuteSpawnCycle), 1f, spawnInterval);
    }

    private void Update()
    {
        _currentCredits += _activeCreditsPerSec * Time.deltaTime;
        
        // CLEANUP: Removes destroyed objects OR objects simply disabled (returned to pool)
        _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
    }

    /// <summary>
    /// Triggered by GameManager.OnLevelChanged or initial Start.
    /// Scales both income and cap exponentially.
    /// </summary>
    public void UpdateDifficultyStats(int level)
    {
        // Income keeps growing (so the Director can buy better/more expensive enemies)
        _activeCreditsPerSec = baseCreditsPerSecond * Mathf.Pow(incomeGrowthMultiplier, level - 1);
        
        // Capacity grows exponentially but HITS A WALL at hardMaxCap
        int calculatedCap = Mathf.RoundToInt(baseMaxCap * Mathf.Pow(capGrowthMultiplier, level - 1));
        _currentMaxCap = Mathf.Min(calculatedCap, hardMaxCap);
        
        Debug.Log($"Director Updated | Level: {level} | Credits/Sec: {_activeCreditsPerSec:F2} | Cap: {_currentMaxCap}/{hardMaxCap}");
    }

    private void ExecuteSpawnCycle()
    {
        // Only spawn if we meet the "Wave" budget
        if (_currentCredits < spawnThreshold) return;

        // Respect cap by including enemies that are still "loading"
        int availableSlots = _currentMaxCap - (_activeEnemies.Count + _pendingSpawnCount);
        if (availableSlots <= 0) return;

        // Ask the Sorter for a manifest of enemies we can afford
        var manifest = _sorter.CreateManifest(database, _currentCredits, availableSlots);

        foreach (var entry in manifest)
        {
            _currentCredits -= entry.cost;
            _pendingSpawnCount++; // Reserve the slot immediately
            StartCoroutine(SpawnRoutine(entry));
        }
    }

    private IEnumerator SpawnRoutine(EnemySpawnerDatabase.EnemyEntry entry)
    {
        Vector3 spawnPos = CalculateSmartPosition(entry.preference);

        // 1. Telegraph/Loading Effect
        if (loadingPrefab != null)
        {
            GameObject loader = Instantiate(loadingPrefab, spawnPos, Quaternion.identity, enemyParent);
            yield return new WaitForSeconds(loadingDuration);
            if (loader != null) Destroy(loader);
        }

        // 2. Pull from Pool
        GameObject enemyObj = EnemyPoolManager.Instance.Get(entry.prefab, spawnPos, Quaternion.identity);
        enemyObj.transform.SetParent(enemyParent); 

        // 3. Initialize the Enemy
        BaseEnemy enemyScript = enemyObj.GetComponent<BaseEnemy>();
        if (enemyScript != null)
        {
            // Set the pool return reference
            enemyScript.sourcePrefab = entry.prefab; 
            
            // Set the level stats from GameManager
            int level = (GameManager.Instance != null) ? GameManager.Instance.CurrentLevel : 1;
            enemyScript.SetLevel(level); 
        }

        _activeEnemies.Add(enemyObj);
        _pendingSpawnCount--;
    }

    private Vector3 CalculateSmartPosition(EnemySpawnerDatabase.SpawnPreference pref)
    {
        if (playerTransform == null) return GetRandomPoint();
        
        // Forced safety zone at the start of the game
        bool inGrace = (Time.time - _gameStartTime) < initialGracePeriod;
        if (inGrace) return GetPointFarFromPlayer();

        switch (pref)
        {
            case EnemySpawnerDatabase.SpawnPreference.NearPlayer: 
                return GetPointNearPlayer();
            case EnemySpawnerDatabase.SpawnPreference.FarFromPlayer: 
                return GetPointFarFromPlayer();
            default: 
                return Random.value < 0.3f ? GetPointNearPlayer() : GetRandomPoint();
        }
    }

    private Vector3 GetPointNearPlayer()
    {
        Vector2Int p = gridManager.GetGridPosition(playerTransform.position);
        int x = Mathf.Clamp(p.x + Random.Range(-2, 3), 0, gridManager.width - 1);
        int y = Mathf.Clamp(p.y + Random.Range(-2, 3), 0, gridManager.height - 1);
        return GridToWorld(x, y);
    }

    private Vector3 GetPointFarFromPlayer()
    {
        Vector2Int p = gridManager.GetGridPosition(playerTransform.position);
        int x, y, attempts = 0;
        do {
            x = Random.Range(0, gridManager.width);
            y = Random.Range(0, gridManager.height);
            attempts++;
        } while (Vector2Int.Distance(new Vector2Int(x, y), p) < 5 && attempts < 10); 
        return GridToWorld(x, y);
    }

    private Vector3 GetRandomPoint() => GridToWorld(Random.Range(0, gridManager.width), Random.Range(0, gridManager.height));

    private Vector3 GridToWorld(int x, int y) 
    {
        Vector3 basePos = gridManager.GetWorldPosition(x, y);
        // Only apply X/Y offset to center in cell; keep Z at 0
        return basePos + new Vector3(gridManager.cellSize * 0.5f, gridManager.cellSize * 0.5f, 0f);
    }
}