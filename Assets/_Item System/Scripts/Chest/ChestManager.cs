using UnityEngine;
using System.Collections.Generic;

public class ChestManager : MonoBehaviour
{
    [Header("Scaling Settings (Spreadsheet)")]
    [Tooltip("Base price for a chest at the start of the game.")]
    [SerializeField] private float baseChestPrice = 10f; 
    [Tooltip("How aggressively prices scale compared to difficulty.")]
    [SerializeField] private float priceExponent = 1.25f; 

    [Header("Grid Settings")]
    public GameObject chestPrefab;
    public int chestCount = 8;
    [Tooltip("Minimum distance in grid cells between chests")]
    public int minDistance = 3;
    [Tooltip("Minimum number of grid cells from any grid edge where chests can spawn.")]
    public int edgeDeadzone = 1;
    public Transform chestParent;

    [Header("References")]
    public GridManager gridManager;

    private List<Vector2Int> _occupiedCells = new List<Vector2Int>();
    private List<Chest> _spawnedChests = new List<Chest>(); // Track for dynamic price updates

    private void OnEnable()
    {
        StageTransitionManager.OnNextStageTriggered += HandleStageTransition;
    }

    private void OnDisable()
    {
        StageTransitionManager.OnNextStageTriggered -= HandleStageTransition;
    }

    void Start()
    {
        if (gridManager == null)
            gridManager = Object.FindFirstObjectByType<GridManager>();

        SpawnChests();
    }

    private void HandleStageTransition()
    {
        ClearChests();
        SpawnChests();
    }

    private void ClearChests()
    {
        _occupiedCells.Clear();
        
        // Instead of destroying, just disable them so we can reuse them
        foreach (Chest chest in _spawnedChests)
        {
            if (chest != null)
                chest.gameObject.SetActive(false);
        }
    }

    private void SpawnChests()
    {
        if (gridManager == null) return;

        int targetSpawnCount = Mathf.Max(0, Random.Range(chestCount - 1, chestCount + 2));

        int spawned = 0;
        int minX = edgeDeadzone;
        int maxX = gridManager.width - 1 - edgeDeadzone;
        int minY = edgeDeadzone;
        int maxY = gridManager.height - 1 - edgeDeadzone;

        int attempts = 0;
        int maxAttempts = 200; 

        while (spawned < targetSpawnCount && attempts < maxAttempts)
        {
            attempts++;

            int randomX = Random.Range(minX, maxX);
            int randomY = Random.Range(minY, maxY);
            Vector2Int potentialCell = new Vector2Int(randomX, randomY);

            if (IsCellValid(potentialCell))
            {
                Vector3 worldPos = GetCenteredWorldPos(randomX, randomY);
                Chest chest = null;

                // Reuse existing chest if available
                if (spawned < _spawnedChests.Count)
                {
                    chest = _spawnedChests[spawned];
                    if (chest != null)
                    {
                        chest.transform.position = worldPos;
                        chest.gameObject.SetActive(true);
                    }
                }
                
                // If not enough chests in pool, create a new one
                if (chest == null)
                {
                    GameObject go = Instantiate(chestPrefab, worldPos, Quaternion.identity, chestParent != null ? chestParent : transform);
                    chest = go.GetComponent<Chest>();
                    if (chest != null)
                    {
                        _spawnedChests.Add(chest);
                    }
                }
                
                if (chest != null)
                {
                    float difficulty = (GameManager.Instance != null && GameManager.Instance.DifficultyCoefficient > 0) 
                        ? GameManager.Instance.DifficultyCoefficient 
                        : 1f;
                    chest.UpdateScalingPrice(baseChestPrice, priceExponent, difficulty);
                }

                _occupiedCells.Add(potentialCell);
                spawned++;
            }
        }
    }

    private bool IsCellValid(Vector2Int cell)
    {
        foreach (var occupied in _occupiedCells)
        {
            float dist = Vector2Int.Distance(cell, occupied);
            if (dist < minDistance)
            {
                return false;
            }
        }
        return true;
    }

    private Vector3 GetCenteredWorldPos(int x, int y)
    {
        Vector3 basePos = gridManager.GetWorldPosition(x, y);
        float offset = gridManager.cellSize * 0.5f;
        return new Vector3(basePos.x + offset, basePos.y + offset, basePos.z);
    }
}