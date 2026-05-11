using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.Enemy;

public class DirectorSpawner2D : MonoBehaviour
{
    [Header("Data & Logic")]
    public EnemySpawnerDatabase database;
    private SpawnSorter _sorter;

    [Header("Economy (Exponential)")]
    public float initialCredits = 25f;
    public float baseCreditsPerSecond = 1.5f;
    public float incomeGrowthMultiplier = 1.12f;
    public float spawnInterval = 3f;

    [Header("Ambush Settings")]
    [Range(0f, 1f), Tooltip("Chance for any enemy to ignore its preference and drop on the player.")]
    public float ambushChance = 0.1f;

    [Header("Capacity & Spawning")]
    public int baseMaxCap = 20;
    public int hardMaxCap = 100;
    public float capGrowthMultiplier = 1.2f;
    public float initialGracePeriod = 10f;
    public GameObject loadingPrefab;
    public float loadingDuration = 1.2f;

    [Header("Pool Settings")]
    public int prewarmCount = 5;

    [Header("References")]
    public GridManager gridManager;
    public Transform playerTransform;
    public Transform enemyParent;

    [Header("Debug Info")]
    [SerializeField] private float _currentCredits;
    [SerializeField] private int _currentMaxCap;
    [SerializeField] private float _activeCreditsPerSec;
    [SerializeField] private int _reservedSlots;
    [SerializeField] private List<GameObject> _activeEnemies = new List<GameObject>();

    private float _graceStartTime;
    private bool _ambushUsedThisCycle = false;
    
    // Throttle enemy cleanup to avoid expensive RemoveAll every frame
    private float _cleanupTimer = 0f;
    private float _cleanupInterval = 0.5f;  // Clean up dead enemies every 0.5 seconds

    private void Awake() => _sorter = new SpawnSorter();

    private void OnEnable()
    {
        GameManager.OnLevelChanged += UpdateDifficultyStats;
        StageTransitionManager.OnNextStageTriggered += HandleStageTransition;
    }

    private void OnDisable()
    {
        GameManager.OnLevelChanged -= UpdateDifficultyStats;
        StageTransitionManager.OnNextStageTriggered -= HandleStageTransition;
    }

    private void HandleStageTransition()
    {
        StopAllCoroutines();
        _graceStartTime = Time.time;
        _reservedSlots = 0;

        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null) continue;

            BaseEnemy enemyScript = enemy.GetComponent<BaseEnemy>();
            if (enemyScript != null && enemyScript.sourcePrefab != null && EnemyPoolManager.Instance != null)
                EnemyPoolManager.Instance.Return(enemyScript.sourcePrefab, enemy);
            else
                enemy.SetActive(false);
        }
        _activeEnemies.Clear();

        if (enemyParent != null)
        {
            foreach (Transform child in enemyParent)
            {
                if (child != null && child.gameObject.activeInHierarchy && child.GetComponent<BaseEnemy>() == null)
                    Destroy(child.gameObject);
            }
        }
    }

    private void Start()
    {
        _graceStartTime = Time.time;
        _currentCredits = initialCredits;
        _reservedSlots = 0;

        if (enemyParent == null) enemyParent = this.transform;

        // Prewarm pool for every enemy type in the database
        if (database != null && EnemyPoolManager.Instance != null)
        {
            foreach (var entry in database.allEnemies)
            {
                if (entry.prefab != null)
                    EnemyPoolManager.Instance.Prewarm(entry.prefab, prewarmCount);
            }
        }

        int startLevel = (GameManager.Instance != null) ? GameManager.Instance.CurrentLevel : 1;
        UpdateDifficultyStats(startLevel);

        InvokeRepeating(nameof(ExecuteSpawnCycle), 1f, spawnInterval);
    }

    private void Update()
    {
        _currentCredits += _activeCreditsPerSec * Time.deltaTime;
        
        // Throttle cleanup to avoid expensive RemoveAll every frame
        _cleanupTimer += Time.deltaTime;
        if (_cleanupTimer >= _cleanupInterval)
        {
            _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
            _cleanupTimer = 0f;
        }
    }

    public void UpdateDifficultyStats(int level)
    {
        _activeCreditsPerSec = baseCreditsPerSecond * Mathf.Pow(incomeGrowthMultiplier, level - 1);
        
        // FIX 1: Calculate as a float first to prevent integer overflow at high levels
        float calculatedCapRaw = baseMaxCap * Mathf.Pow(capGrowthMultiplier, level - 1);
        
        // Clamp the float against your hard max cap to ensure it never exceeds your limit
        float clampedCap = Mathf.Min(calculatedCapRaw, hardMaxCap);
        
        // Convert to integer AFTER clamping
        _currentMaxCap = Mathf.RoundToInt(clampedCap);
    }

    private void ExecuteSpawnCycle()
    {
        if (database == null) return;

        var cheapest = database.GetCheapest();
        if (cheapest == null || _currentCredits < cheapest.cost) return;

        // FIX 2: Safely clamp available slots so it can never drop below zero (prevents underflow logic bugs)
        int availableSlots = Mathf.Max(0, _currentMaxCap - _activeEnemies.Count - _reservedSlots);
        
        if (availableSlots <= 0) return;

        _ambushUsedThisCycle = false;

        var manifest = _sorter.CreateManifest(database, _currentCredits, availableSlots);
        if (manifest.Count == 0) return;

        // Lock slots immediately so the next cycle doesn't double-count
        _reservedSlots += manifest.Count;

        foreach (var entry in manifest)
            _currentCredits -= entry.cost;

        StartCoroutine(SpawnManifestRoutine(manifest));
    }

    private IEnumerator SpawnManifestRoutine(List<EnemySpawnerDatabase.EnemyEntry> manifest)
    {
        foreach (var entry in manifest)
        {
            StartCoroutine(SpawnRoutine(entry));
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator SpawnRoutine(EnemySpawnerDatabase.EnemyEntry entry)
    {
        bool isAmbush = !_ambushUsedThisCycle && Random.value < ambushChance;
        if (isAmbush) _ambushUsedThisCycle = true;

        Vector3 spawnPos = CalculateSmartPosition(entry.preference, isAmbush);

        if (loadingPrefab != null)
        {
            GameObject loader = Instantiate(loadingPrefab, spawnPos, Quaternion.identity, enemyParent);
            yield return new WaitForSeconds(loadingDuration);
            if (loader != null) Destroy(loader);
        }

        GameObject enemyObj = EnemyPoolManager.Instance.Get(entry.prefab, spawnPos, Quaternion.identity);
        enemyObj.transform.SetParent(enemyParent);

        BaseEnemy enemyScript = enemyObj.GetComponent<BaseEnemy>();
        if (enemyScript != null)
        {
            enemyScript.sourcePrefab = entry.prefab;
            int level = (GameManager.Instance != null) ? GameManager.Instance.CurrentLevel : 1;
            enemyScript.SetLevel(level);
        }

        _activeEnemies.Add(enemyObj);

        // Release the reservation now that this enemy is fully tracked
        _reservedSlots = Mathf.Max(0, _reservedSlots - 1);
    }

    private Vector3 CalculateSmartPosition(EnemySpawnerDatabase.SpawnPreference pref, bool forceAmbush)
    {
        if (playerTransform == null) return GetRandomPoint();

        bool inGrace = (Time.time - _graceStartTime) < initialGracePeriod;
        if (inGrace) return GetPointFarFromPlayer();

        if (forceAmbush || pref == EnemySpawnerDatabase.SpawnPreference.OnPlayer)
            return GetPointOnPlayer();

        switch (pref)
        {
            case EnemySpawnerDatabase.SpawnPreference.NearPlayer:
                return GetPointNearPlayer();
            case EnemySpawnerDatabase.SpawnPreference.FarFromPlayer:
                return GetPointFarFromPlayer();
            case EnemySpawnerDatabase.SpawnPreference.Random:
            default:
                float roll = Random.value;
                if (roll < 0.1f) return GetPointOnPlayer();
                if (roll < 0.4f) return GetPointNearPlayer();
                return GetRandomPoint();
        }
    }

    private Vector3 GetPointOnPlayer()
    {
        Vector2Int p = gridManager.GetGridPosition(playerTransform.position);
        return GridToWorld(p.x, p.y);
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

    private Vector3 GetRandomPoint() =>
        GridToWorld(Random.Range(0, gridManager.width), Random.Range(0, gridManager.height));

    private Vector3 GridToWorld(int x, int y)
    {
        Vector3 basePos = gridManager.GetWorldPosition(x, y);
        return basePos + new Vector3(gridManager.cellSize * 0.5f, gridManager.cellSize * 0.5f, 0f);
    }

    public void DisablePercentageOfEnemies(float percentage)
    {
        int enemiesToDisable = Mathf.RoundToInt(_activeEnemies.Count * percentage);
        for (int i = 0; i < enemiesToDisable && i < _activeEnemies.Count; i++)
        {
            if (_activeEnemies[i] != null)
                _activeEnemies[i].SetActive(false);
        }
    }

    public void WipeAllEnemies()
    {
        _reservedSlots = 0;

        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null) continue;

            BaseEnemy enemyScript = enemy.GetComponent<BaseEnemy>();
            if (enemyScript != null && enemyScript.sourcePrefab != null && EnemyPoolManager.Instance != null)
                EnemyPoolManager.Instance.Return(enemyScript.sourcePrefab, enemy);
            else
                enemy.SetActive(false);
        }
        _activeEnemies.Clear();
    }
}