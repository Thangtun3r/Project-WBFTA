using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    public Vector2Int CurrentGridCell { get; private set; }
    protected bool isRegistered;

    protected virtual void OnEnable()
    {
        RegisterWithManager();
    }

    protected virtual void Start()
    {
        if (!isRegistered) RegisterWithManager();
    }

    private void RegisterWithManager()
    {
        if (ItemCollectibleManager.Instance == null) return;

        CurrentGridCell = ItemCollectibleManager.Instance.GetCellFromWorldPos(transform.position);
        ItemCollectibleManager.Instance.RegisterItem(this);
        isRegistered = true;
    }

    protected virtual void OnDisable()
    {
        if (isRegistered)
        {
            ItemCollectibleManager.Instance.UnregisterItem(this);
            isRegistered = false;
        }
    }

    public void UpdateGridStatus()
    {
        Vector2Int newCell = ItemCollectibleManager.Instance.GetCellFromWorldPos(transform.position);

        if (newCell != CurrentGridCell)
        {
            ItemCollectibleManager.Instance.UpdateItemCell(this, CurrentGridCell, newCell);
            CurrentGridCell = newCell;
        }
    }

    public abstract void OnCollected(GameObject collector);
}