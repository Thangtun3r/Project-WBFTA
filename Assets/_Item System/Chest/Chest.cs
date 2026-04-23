using System;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class Chest : MonoBehaviour
{
    public static event Action<Transform> OnChestHover;
    public static event Action<Transform> OnChestExit;
    public static event Action<Transform> OnChestClick;

    [SerializeField] private GameObject ChestVisual;
    
    [Header("Settings")]
    [SerializeField] private float clickPunchStrength = 10f;
    [SerializeField] private int moneyRequired = 0;
    [SerializeField] private GameObject chestInfoGameObject;
    [SerializeField] private TextMeshProUGUI moneyText;
    
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private bool isPlayerInside = false;
    private bool isOpened = false;

    private void Awake()
    {
        if (ChestVisual != null)
        {
            originalScale = ChestVisual.transform.localScale;
            originalPosition = ChestVisual.transform.localPosition;
        }
    }

    private void OnEnable()
    {
        ResetChest();
    }

    private void ResetChest()
    {
        if (ChestVisual != null)
        {
            ChestVisual.transform.DOKill();
            
            SpriteRenderer spriteRenderer = ChestVisual.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            ChestVisual.transform.localScale = originalScale;
            ChestVisual.transform.localPosition = originalPosition;
            ChestVisual.transform.rotation = Quaternion.identity;
        }

        if (chestInfoGameObject != null)
        {
            chestInfoGameObject.SetActive(true);
        }

        if (moneyText != null)
        {
            moneyText.text = moneyRequired.ToString() + "$";
        }

        isPlayerInside = false;
        isOpened = false;
    }

    private void Update()
    {
        if (isPlayerInside && !isOpened && Input.GetMouseButtonDown(0))
        {
            TryOpenChest();
        }
    }

    private void TryOpenChest()
    {
        if (EconomyManager.Instance.CurrentMoney < moneyRequired)
        {
            PlayInsufficientFundsAnimation();
            return;
        }

        if (!EconomyManager.Instance.TryRemoveMoney(moneyRequired))
        {
            PlayInsufficientFundsAnimation();
            return;
        }

        isOpened = true;
        PlayClickAnimation();
        TintVisualGray();
        DisableChestInfo();
        OnChestClick?.Invoke(transform);
    }

    private void PlayInsufficientFundsAnimation()
    {
        var target = ChestVisual.transform;
        
        target.DOKill(true);
        target.localPosition = originalPosition;

        SpriteRenderer spriteRenderer = ChestVisual.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))
                .Append(spriteRenderer.DOColor(Color.white, 0.1f));
        }

        // Changed OnTerminate to OnKill
        target.DOShakePosition(0.4f, strength: new Vector3(0.3f, 0f, 0f), vibrato: 10, randomness: 0.5f)
            .OnComplete(() => target.localPosition = originalPosition)
            .OnKill(() => target.localPosition = originalPosition);
    }

    private void TintVisualGray()
    {
        SpriteRenderer spriteRenderer = ChestVisual.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0x6A / 255f, 0x6A / 255f, 0x6A / 255f, 1f);
        }
    }

    private void DisableChestInfo()
    {
        if (chestInfoGameObject != null)
        {
            chestInfoGameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isOpened)
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
        target.DOScale(originalScale * 1.1f, 0.25f).SetEase(Ease.OutBack);
    }

    private void PlayClickAnimation()
    {
        var target = ChestVisual.transform;
        target.DOKill(true);
        
        // Changed OnTerminate to OnKill
        target.DOPunchRotation(new Vector3(0, 0, clickPunchStrength), 0.2f, 10, 1f)
              .OnKill(() => target.rotation = Quaternion.identity);
    }

    private void ResetToIdle()
    {
        var target = ChestVisual.transform;
        target.DOKill();

        target.DOScale(originalScale, 0.2f).SetEase(Ease.OutSine);
        target.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutSine);
        target.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutSine);
    }
}