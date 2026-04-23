using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 20;
    public int height = 12;
    public float cellSize = 1f;
    public Color defaultColor = Color.cyan;
    public Color activeColor = Color.red;

    [Header("Grid Bounds")]
    [SerializeField] private bool useGridBounds = false;
    [SerializeField] private GameObject gridBoundsObject;

    [Header("References")]
    public Transform playerTransform;

    [Header("Gizmo Settings")]
    public bool drawGizmos = true;

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        // Update grid dimensions from bounds object if enabled
        if (useGridBounds && gridBoundsObject != null)
        {
            Vector3 boundsScale = gridBoundsObject.transform.localScale;
            width = Mathf.Max(1, Mathf.RoundToInt(boundsScale.x / cellSize));
            height = Mathf.Max(1, Mathf.RoundToInt(boundsScale.y / cellSize));
        }

        // Fallback: Try to find player if reference is null (useful for Editor testing)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Calculate player's current grid coordinate once per frame
        Vector2Int playerGridPos = new Vector2Int(-1, -1);
        if (playerTransform != null)
        {
            playerGridPos = GetGridPosition(playerTransform.position);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Toggle color if this cell matches the player's grid position
                Gizmos.color = (x == playerGridPos.x && y == playerGridPos.y) ? activeColor : defaultColor;

                Vector3 pos = GetWorldPosition(x, y);
                Vector3 center = pos + new Vector3(cellSize, cellSize, 0f) * 0.5f;
                Vector3 size = new Vector3(cellSize, cellSize, 0f);
                
                Gizmos.DrawWireCube(center, size);
            }
        }
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 relativePos = worldPosition - transform.position;
        int x = Mathf.FloorToInt(relativePos.x / cellSize);
        int y = Mathf.FloorToInt(relativePos.y / cellSize);

        // We don't clamp here so the grid stays cyan if the player leaves the bounds
        return new Vector2Int(x, y);
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y) * cellSize + transform.position;
    }

    private void Update()
    {
        // Update grid dimensions at runtime if bounds object is assigned and toggle is on
        if (useGridBounds && gridBoundsObject != null)
        {
            Vector3 boundsScale = gridBoundsObject.transform.localScale;
            width = Mathf.Max(1, Mathf.RoundToInt(boundsScale.x / cellSize));
            height = Mathf.Max(1, Mathf.RoundToInt(boundsScale.y / cellSize));
        }
    }
}