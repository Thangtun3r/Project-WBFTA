using UnityEngine;
using DG.Tweening;

public class ItemDrop : Collectible
{
    private string itemId;
    private PlayerInventory playerInventory;
    
    [SerializeField] private GameObject visualObject;
    [SerializeField] private GameObject shadowObject;
    [SerializeField] private float pickupRadius = 2f;
    
    [Header("Floating Animation")]
    [SerializeField] private float floatingHeight = 0.5f;
    [SerializeField] private float floatingDuration = 2f;
    
    [Header("Landing Animation")]
    [SerializeField] private float landingDuration = 0.5f;
    
    [Header("Pickup Animation")]
    [SerializeField] private float pickupDuration = 0.1f;

    private void Awake()
    {
        if (visualObject == null)
        {
            visualObject = gameObject;
        }
    }

    private void OnEnable()
    {
        PlayFloatingAnimation();
    }

    public void Initialize(string itemId)
    {
        this.itemId = itemId;

        // Get item definition and set the sprite
        ItemDefinition definition = ItemDatabaseFactory.Instance.GetDefinition(itemId);
        if (definition != null && visualObject != null)
        {
            SpriteRenderer spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = definition.icon;
            }
            else
            {
                Debug.LogWarning("ItemDrop: Visual object does not have a SpriteRenderer.");
            }
        }
        else if (definition == null)
        {
            Debug.LogWarning($"ItemDrop: Could not find definition for item '{itemId}'.");
        }
    }

    private void PlayFloatingAnimation()
    {
        if (visualObject == null) return;

        visualObject.transform.DOKill();
        Vector3 startPos = visualObject.transform.localPosition;
        Vector3 floatPos = startPos + Vector3.up * floatingHeight;

        visualObject.transform.DOLocalMove(floatPos, floatingDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public override void OnCollected(GameObject collector)
    {
        PlayerInventory inventory = collector.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.ProcessPickup(itemId);
            Destroy(gameObject);
        
        }
        else
        {
            Debug.LogWarning("ItemDrop: Collector does not have a PlayerInventory component.");
        }



    }
}