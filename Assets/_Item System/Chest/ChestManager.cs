using UnityEngine;
using System.Collections.Generic;

public class ChestManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject chestPrefab;
    public int chestCount = 8;
    [Tooltip("Minimum distance in grid cells between chests")]
    public int minDistance = 3; 
    public Transform chestParent;

    [Header("References")]
    public GridManager gridManager;

    private List<Vector2Int> _occupiedCells = new List<Vector2Int>();

    void Start()
    {
        if (gridManager == null)
            gridManager = Object.FindFirstObjectByType<GridManager>();

        SpawnChests();
    }

    public void SpawnChests()
    {
        if (chestPrefab == null || gridManager == null) return;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 200; // Increased safety break for distance logic

        while (spawned < chestCount && attempts < maxAttempts)
        {
            attempts++;

            int randomX = Random.Range(0, gridManager.width);
            int randomY = Random.Range(0, gridManager.height);
            Vector2Int potentialCell = new Vector2Int(randomX, randomY);

            // NEW: Check if this cell is far enough from all existing chests
            if (IsCellValid(potentialCell))
            {
                Vector3 worldPos = GetCenteredWorldPos(randomX, randomY);
                Instantiate(chestPrefab, worldPos, Quaternion.identity, chestParent != null ? chestParent : transform);
                
                _occupiedCells.Add(potentialCell);
                spawned++;
            }
        }
    }

    private bool IsCellValid(Vector2Int cell)
    {
        foreach (var occupied in _occupiedCells)
        {
            // Manhattan distance check (cheaper) or use Vector2Int.Distance
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
        return basePos + new Vector3(offset, offset, 0f);
    }
}