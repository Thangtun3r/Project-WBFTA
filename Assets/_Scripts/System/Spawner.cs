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
        [Range(1f, 100f)]
        public float weight;
    }

    [Header("Director Settings")]
    public List<EnemyDefinition> enemyPool;
    public float initialCredits = 10f;
    public float baseCreditsPerSecond = 1f;
    public float difficultyScaling = 0.5f; 
    public int globalMaxCap = 30;
    public int reservedPlayerSlots = 5; // Guaranteed slots for player spawns
    public float spawnInterval = 2f;
    public float capGrowthMultiplier = 1.2f; // Exponential growth per level
    
    private int _baseGlobalMaxCap; // Store base cap for scaling
    
    [Header("Telegraph Settings")]
    public GameObject loadingPrefab;
    public float loadingDuration = 1.5f;

    [Header("Spawn Logic")]
    [Range(0f, 1f)]
    public float nearPlayerChance = 0.3f;
    public float playerVicinityRange = 3f;

    [Header("References")]
    public GridManager gridManager;
    public Transform playerTransform;

    private float _currentCredits;
    private float _activeCreditsPerSecond;
    private List<GameObject> _activeEnemies = new List<GameObject>();

    private void OnEnable() => GameManager.OnLevelChanged += UpdateDifficulty;
    private void OnDisable() => GameManager.OnLevelChanged -= UpdateDifficulty;

    private void Start()
    {
        if (playerTransform == null) 
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        _baseGlobalMaxCap = globalMaxCap;
        _currentCredits = initialCredits;
        UpdateDifficulty(GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1);
        InvokeRepeating(nameof(TrySpawnEnemy), 0.5f, spawnInterval);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;
        _currentCredits += _activeCreditsPerSecond * Time.deltaTime;
        _activeEnemies.RemoveAll(item => item == null || !item.activeInHierarchy);
    }

    private void UpdateDifficulty(int level)
    {
        _activeCreditsPerSecond = baseCreditsPerSecond + (level - 1) * difficultyScaling;
        globalMaxCap = Mathf.RoundToInt(_baseGlobalMaxCap * Mathf.Pow(capGrowthMultiplier, level - 1));
    }

    private void TrySpawnEnemy()
    {
        if (_activeEnemies.Count >= globalMaxCap || enemyPool.Count == 0) return;

        bool isNearPlayerSpawn = Random.value < nearPlayerChance;
        int nonPlayerEnemies = GetNonPlayerEnemyCount();

        // If trying to spawn a random "map" enemy but map is full (respecting reservation)
        if (!isNearPlayerSpawn && nonPlayerEnemies >= (globalMaxCap - reservedPlayerSlots))
        {
            return; 
        }

        EnemyDefinition selection = enemyPool[Random.Range(0, enemyPool.Count)];

        if (_currentCredits >= selection.weight)
        {
            _currentCredits -= selection.weight;
            StartCoroutine(SpawnRoutine(selection, isNearPlayerSpawn));
        }
    }

    private int GetNonPlayerEnemyCount()
    {
        int count = 0;
        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(enemy.transform.position, playerTransform.position);
            if (dist > playerVicinityRange) count++;
        }
        return count;
    }

    private IEnumerator SpawnRoutine(EnemyDefinition enemyDef, bool nearPlayer)
    {
        Vector3 spawnPos = CalculateSpawnPoint(nearPlayer);

        if (loadingPrefab != null)
        {
            GameObject loader = Instantiate(loadingPrefab, spawnPos, Quaternion.identity, transform);
            loader.transform.localPosition = new Vector3(loader.transform.localPosition.x, loader.transform.localPosition.y, 0f);
            yield return new WaitForSeconds(loadingDuration);
            Destroy(loader);
        }

        GameObject enemyObj = Instantiate(enemyDef.prefab, spawnPos, Quaternion.identity, transform);
        enemyObj.transform.localPosition = new Vector3(enemyObj.transform.localPosition.x, enemyObj.transform.localPosition.y, 0f);
        
        BaseEnemy enemyScript = enemyObj.GetComponent<BaseEnemy>();
        if (enemyScript != null && GameManager.Instance != null)
            enemyScript.SetLevel(GameManager.Instance.CurrentLevel);
        
        _activeEnemies.Add(enemyObj);
    }

    private Vector3 CalculateSpawnPoint(bool nearPlayer)
    {
        int targetX, targetY;

        if (nearPlayer && playerTransform != null)
        {
            Vector2Int playerGrid = gridManager.GetGridPosition(playerTransform.position);
            targetX = Mathf.Clamp(playerGrid.x + Random.Range(-2, 3), 0, gridManager.width - 1);
            targetY = Mathf.Clamp(playerGrid.y + Random.Range(-2, 3), 0, gridManager.height - 1);
        }
        else
        {
            targetX = Random.Range(0, gridManager.width);
            targetY = Random.Range(0, gridManager.height);
        }

        float halfCell = gridManager.cellSize * 0.5f;
        return gridManager.GetWorldPosition(targetX, targetY) + new Vector3(halfCell, halfCell, 0);
    }
}