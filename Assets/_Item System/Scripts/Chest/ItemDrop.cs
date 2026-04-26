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
        
        // Find the player inventory in the scene
        playerInventory = FindObjectOfType<PlayerInventory>();
        
        if (playerInventory == null)
        {
            Debug.LogWarning("ItemDrop: No PlayerInventory found in scene.");
        }

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

        // Animate shadow scale on landing
        PlayLandingAnimation();
    }

    private void PlayLandingAnimation()
    {
        if (shadowObject != null)
        {
            SpriteRenderer shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
            if (shadowRenderer != null)
            {
            
            

                //shadowRenderer.DOFade(1f, landingDuration).SetEase(Ease.OutBack);
            }
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

    public override void OnCollected()
    {
        // Kill floating animation
        if (visualObject != null)
        {
            visualObject.transform.DOKill();
        }

        // Scale down to 0 and destroy
        if (visualObject != null)
        {
            visualObject.transform.DOScale(Vector3.zero, pickupDuration).SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    if (playerInventory != null)
                    {
                        playerInventory.ProcessPickup(itemId);
                    }
                    Destroy(gameObject);
                });
        }
        else
        {
            if (playerInventory != null)
            {
                playerInventory.ProcessPickup(itemId);
            }
            Destroy(gameObject);
        }
    }
}