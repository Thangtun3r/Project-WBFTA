using System;
using UnityEngine;
using DG.Tweening;

public class Chest : MonoBehaviour
{
    public static event Action<Transform> OnChestHover;
    public static event Action<Transform> OnChestExit;
    public static event Action<Transform> OnChestClick;

    [SerializeField] private GameObject ChestVisual;
    [SerializeField] private GameObject itemDropPrefab;
    
    [Header("Settings")]
    [SerializeField] private float clickPunchStrength = 10f;
    
    private Vector3 originalScale;
    private bool isPlayerInside = false;

    private void Awake()
    {
        // Store the default scale set in the Inspector
        if (ChestVisual != null)
            originalScale = ChestVisual.transform.localScale;
    }

    private void Update()
    {
        if (isPlayerInside && Input.GetMouseButtonDown(0))
        {
            PlayClickAnimation();
            SpawnItemDrop();
            OnChestClick?.Invoke(transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            PlayHoverAnimation();
            OnChestHover?.Invoke(transform);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            ResetToIdle();
            OnChestExit?.Invoke(transform);
        }
    }

    private void PlayHoverAnimation()
    {
        var target = ChestVisual.transform;
        target.DOKill();

        // 1.1x the original scale
        target.DOScale(originalScale * 1.1f, 0.25f).SetEase(Ease.OutBack);
    }

    private void PlayClickAnimation()
    {
        var target = ChestVisual.transform;
        
        // Kill previous and snap to current hover rotation for consistency
        target.DOKill(true);

        // DOPunchRotation is consistent/non-random compared to Shake
        // It will "kick" the rotation and wobble back to the start point
        target.DOPunchRotation(new Vector3(0, 0, clickPunchStrength), 0.2f, 10, 1f);
    }

    private void SpawnItemDrop()
    {
        if (itemDropPrefab == null)
        {
            Debug.LogWarning("Chest: ItemDropPrefab is not assigned.");
            return;
        }

        string randomItemId = ItemDatabaseFactory.Instance.GetRandomItemId();
        if (randomItemId == null)
        {
            Debug.LogWarning("Chest: Could not get a random item from the database.");
            return;
        }

        GameObject spawnedDrop = Instantiate(itemDropPrefab, transform.position, Quaternion.identity);
        ItemDrop itemDrop = spawnedDrop.GetComponent<ItemDrop>();
        
        if (itemDrop != null)
        {
            itemDrop.Initialize(randomItemId);
            
            // Animate the item in an arc upward and outward
            Vector3 landPosition = transform.position + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, 0f);
            spawnedDrop.transform.DOJump(landPosition, jumpPower: 2f, numJumps: 1, duration: 0.6f).SetEase(Ease.OutQuad);
        }
        else
        {
            Debug.LogWarning("Chest: Spawned prefab does not have an ItemDrop component.");
        }
    }

    private void ResetToIdle()
    {
        var target = ChestVisual.transform;
        target.DOKill();

        // Return to the exact original scale and zero rotation
        target.DOScale(originalScale, 0.2f).SetEase(Ease.OutSine);
        target.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutSine);
    }
}