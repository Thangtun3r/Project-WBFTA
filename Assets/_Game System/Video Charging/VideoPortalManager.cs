using UnityEngine;

public class VideoPortalManager : MonoBehaviour
{
    [Header("Portal Settings")]
    public GameObject portalPrefab;
    [Tooltip("Minimum distance in grid units from the player")]
    public float minDistanceFromPlayer = 7f;
    public Transform portalParent;

    [Header("References")]
    public GridManager gridManager;
    public Transform playerTransform;

    [Header("Spawn Settings")]
    [Tooltip("Minimum distance in grid units from any grid edge.")]
    public int edgeDeadzone = 2;

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
        // Auto-assign if missing
        if (gridManager == null)
            gridManager = Object.FindFirstObjectByType<GridManager>();

        SpawnPortal();
    }

    private void HandleStageTransition()
    {
        ClearPortals();
        SpawnPortal();
    }

    [ContextMenu("Test Spawn Portal")]
    private void TestSpawnPortal()
    {
        ClearPortals();
        SpawnPortal();
    }

    private void ClearPortals()
    {
        Transform parent = portalParent != null ? portalParent : transform;
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    public void SpawnPortal()
    {
        if (portalPrefab == null || gridManager == null || playerTransform == null) return;

        // 1. Get player's current grid position
        Vector2Int playerGridPos = gridManager.GetGridPosition(playerTransform.position);
        Vector2Int spawnCell = Vector2Int.zero;
        bool validSpotFound = false;
        int attempts = 0;

        // 2. Search for a spot far enough away
        int minX = Mathf.Clamp(edgeDeadzone, 0, gridManager.width - 1);
        int maxX = Mathf.Clamp(gridManager.width - edgeDeadzone, 0, gridManager.width);
        int minY = Mathf.Clamp(edgeDeadzone, 0, gridManager.height - 1);
        int maxY = Mathf.Clamp(gridManager.height - edgeDeadzone, 0, gridManager.height);

        if (minX >= maxX || minY >= maxY)
        {
            Debug.LogWarning("VideoPortalManager: edgeDeadzone is too large for the current grid size.");
            return;
        }

        while (!validSpotFound && attempts < 100)
        {
            attempts++;
            
            int x = Random.Range(minX, maxX);
            int y = Random.Range(minY, maxY);
            spawnCell = new Vector2Int(x, y);

            // Calculate distance in grid units
            float dist = Vector2Int.Distance(spawnCell, playerGridPos);
            
            if (dist >= minDistanceFromPlayer)
            {
                validSpotFound = true;
            }
        }

        // 3. Spawn the portal if a spot was found
        if (validSpotFound)
        {
            Vector3 worldPos = GetCenteredWorldPos(spawnCell.x, spawnCell.y);
            Instantiate(portalPrefab, worldPos, Quaternion.identity, portalParent != null ? portalParent : transform);
        }
        else
        {
            Debug.LogWarning("VideoPortalManager: Failed to find spawn spot far from player after 100 attempts.");
        }
    }

    private Vector3 GetCenteredWorldPos(int x, int y)
    {
        // Get the bottom-left corner of the cell from GridManager
        Vector3 basePos = gridManager.GetWorldPosition(x, y);
        
        // Add the half-cell offset to center the portal (Z stays at 0)
        float offset = gridManager.cellSize * 0.5f;
        return basePos + new Vector3(offset, offset, 0f);
    }
}