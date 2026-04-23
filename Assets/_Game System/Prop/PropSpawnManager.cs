
using UnityEngine;
using System.Collections.Generic;

public class PropSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] propPrefabs;
    public int propCount = 5;
    [Tooltip("Minimum distance in grid cells between props")]
    public int minDistance = 2;
    public Transform propParent;

    [Header("References")]
    public GridManager gridManager;

    private List<Vector2Int> _occupiedCells = new List<Vector2Int>();

    private void OnEnable()
    {
        StageTransitionManager.OnNextStageTriggered += HandleStageTransition;
        GameManager.OnLevelChanged += UpdatePropLevels;
    }

    private void OnDisable()
    {
        StageTransitionManager.OnNextStageTriggered -= HandleStageTransition;
        GameManager.OnLevelChanged -= UpdatePropLevels;
    }

    void Start()
    {
        if (gridManager == null)
            gridManager = Object.FindFirstObjectByType<GridManager>();

        SpawnProps();
    }

    private void HandleStageTransition()
    {
        ClearProps();
        SpawnProps();
    }

    private void UpdatePropLevels(int level)
    {
        Transform parent = propParent != null ? propParent : transform;
        foreach (Transform child in parent)
        {
            PropBase prop = child.GetComponent<PropBase>();
            if (prop != null)
            {
                prop.SetLevel(level);
            }
        }
    }

    private void ClearProps()
    {
        _occupiedCells.Clear();
        Transform parent = propParent != null ? propParent : transform;
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    public void SpawnProps()
    {
        if (propPrefabs == null || propPrefabs.Length == 0 || gridManager == null) return;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 200;

        while (spawned < propCount && attempts < maxAttempts)
        {
            attempts++;

            int randomX = Random.Range(0, gridManager.width);
            int randomY = Random.Range(0, gridManager.height);
            Vector2Int potentialCell = new Vector2Int(randomX, randomY);

            if (IsCellValid(potentialCell))
            {
                Vector3 worldPos = GetCenteredWorldPos(randomX, randomY);
                int randomPrefabIndex = Random.Range(0, propPrefabs.Length);
                GameObject spawnedProp = Instantiate(propPrefabs[randomPrefabIndex], worldPos, Quaternion.identity, propParent != null ? propParent : transform);

                PropBase propScript = spawnedProp.GetComponent<PropBase>();
                if (propScript != null)
                {
                    int currentLevel = GameManager.Instance?.CurrentLevel ?? 1;
                    propScript.SetLevel(currentLevel);
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
        return basePos + new Vector3(offset, offset, 0f);
    }
}