using System.Collections.Generic;
using UnityEngine;

public class ItemCollectibleManager : MonoBehaviour
{
    public static ItemCollectibleManager Instance { get; private set; }
    
    [SerializeField] private GridManager gridManager;
    
    // The Spatial Hash: Maps a Grid Coordinate to a list of items in that spot
    private Dictionary<Vector2Int, List<Collectible>> spatialHash = new Dictionary<Vector2Int, List<Collectible>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (gridManager == null) gridManager = GetComponent<GridManager>();
    }

    public void RegisterItem(Collectible item)
    {
        Vector2Int cell = item.CurrentGridCell;
        if (!spatialHash.ContainsKey(cell)) spatialHash[cell] = new List<Collectible>();
        
        spatialHash[cell].Add(item);
    }

    public void UnregisterItem(Collectible item)
    {
        Vector2Int cell = item.CurrentGridCell;
        if (spatialHash.ContainsKey(cell))
        {
            spatialHash[cell].Remove(item);
        }
    }

    public void UpdateItemCell(Collectible item, Vector2Int oldCell, Vector2Int newCell)
    {
        if (spatialHash.ContainsKey(oldCell)) spatialHash[oldCell].Remove(item);

        if (!spatialHash.ContainsKey(newCell)) spatialHash[newCell] = new List<Collectible>();
        spatialHash[newCell].Add(item);
    }

    // Helper to bridge between the Item and the GridManager's math
    public Vector2Int GetCellFromWorldPos(Vector3 position)
    {
        return gridManager.GetGridPosition(position);
    }

    // The optimized search: Only returns items in the player's cell and 8 neighbors
    public List<Collectible> GetNearbyItems(Vector3 worldPosition)
    {
        Vector2Int centerCell = GetCellFromWorldPos(worldPosition);
        List<Collectible> nearbyItems = new List<Collectible>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int targetCell = centerCell + new Vector2Int(x, y);
                if (spatialHash.TryGetValue(targetCell, out List<Collectible> cellList))
                {
                    nearbyItems.AddRange(cellList);
                }
            }
        }
        return nearbyItems;
    }
}