using UnityEngine;
using DG.Tweening;

public class ItemDropSpawner : MonoBehaviour
{
    [SerializeField] private GameObject itemDropPrefab;

    private void OnEnable()
    {
        Chest.OnChestClick += HandleChestClicked;
    }

    private void OnDisable()
    {
        Chest.OnChestClick -= HandleChestClicked;
    }

    private void HandleChestClicked(Transform chestTransform)
    {
        SpawnItemDrop(chestTransform.position);
    }

    private void SpawnItemDrop(Vector3 spawnPosition)
    {
        if (itemDropPrefab == null)
        {
            Debug.LogWarning("ItemDropSpawner: ItemDropPrefab is not assigned.");
            return;
        }

        string randomItemId = ItemDatabaseFactory.Instance.GetRandomItemId();
        if (randomItemId == null)
        {
            Debug.LogWarning("ItemDropSpawner: Could not get a random item from the database.");
            return;
        }

        GameObject spawnedDrop = Instantiate(itemDropPrefab, spawnPosition, Quaternion.identity);
        ItemDrop itemDrop = spawnedDrop.GetComponent<ItemDrop>();
        
        if (itemDrop != null)
        {
            itemDrop.Initialize(randomItemId);
            
            // Animate the item in an arc upward and outward
            Vector3 landPosition = spawnPosition + new Vector3(Random.Range(-2f, 2f), 0f, 0f);
            spawnedDrop.transform.DOJump(landPosition, jumpPower: 2f, numJumps: 1, duration: 0.6f).SetEase(Ease.OutQuad);
        }
        else
        {
            Debug.LogWarning("ItemDropSpawner: Spawned prefab does not have an ItemDrop component.");
        }
    }
}
