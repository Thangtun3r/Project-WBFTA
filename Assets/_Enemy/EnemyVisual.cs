using UnityEngine;
using DG.Tweening;
using _Scripts.Enemy;

public class EnemyVisual : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The object that will scale and shake (e.g., the 'GFX' child)")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("The shadow object that offsets when dragged")]
    [SerializeField] private Transform visualShadow;
    [Tooltip("How much the shadow offsets when dragged (e.g. 10% offset = 0.1f)")]
    private Vector3 dragShadowOffset = new Vector3(0.05f, -0.05f, 0f);
    [SerializeField] public SpriteRenderer[] spriteRenderers;

    private const float FlashDuration = 0.1f;
    private const float PunchAmount = 0.25f;
    private const float PunchDuration = 0.03f;
    private const float ShakeStrength = 0.1f;
    private const float ShakeDuration = 0.1f;
    private const int ShakeVibrato = 10;
    private const string DeathEffectName = "enemyDeath";
    private const string HitEffectName = "enemyHit";
    private static readonly Vector3 ShakeRotationAngle = new Vector3(0f, 0f, 15f);

    private BaseEnemy parentEnemy;
    private MaterialPropertyBlock mpb;
    private Vector3 originalScale;
    private Vector3 originalLocalPos; // New: Cache for wiggle
    private Quaternion originalLocalRotation;
    private Vector3 originalShadowPos;
    private static readonly int FlashProperty = Shader.PropertyToID("_Flash");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        parentEnemy = GetComponentInParent<BaseEnemy>();

        if (visualRoot == null) visualRoot = transform;
        
        // Cache initial transforms
        originalScale = visualRoot.localScale;
        originalLocalPos = visualRoot.localPosition;
        originalLocalRotation = visualRoot.localRotation;
        
        if (visualShadow != null)
        {
            originalShadowPos = visualShadow.localPosition;
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ResetVisuals();

        if (parentEnemy != null)
        {
            parentEnemy.OnEnemyHit += HandleHitVisuals;
            parentEnemy.OnEnemyDeath_Local += HandleDeathVisuals;
        }
    }

    private void OnDisable()
    {
        if (parentEnemy != null)
        {
            parentEnemy.OnEnemyHit -= HandleHitVisuals;
            parentEnemy.OnEnemyDeath_Local -= HandleDeathVisuals;
        }

        // Clean up all tweens on these targets
        visualRoot.DOKill();
        DOTween.Kill(this);
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        visualRoot.localScale = originalScale;
        visualRoot.localPosition = originalLocalPos; // Reset position
        visualRoot.localRotation = originalLocalRotation;
        
        if (visualShadow != null)
        {
            visualShadow.DOKill();
            visualShadow.localPosition = originalShadowPos;
        }
        
        SetFlashOnAll(0f);
    }

    public void SetDragState(bool isDragging)
    {
        if (visualShadow != null)
        {
            visualShadow.DOKill();
            if (isDragging)
            {
                visualShadow.DOLocalMove(originalShadowPos + dragShadowOffset, 0.1f).SetEase(Ease.OutQuad);
            }
            else
            {
                visualShadow.DOLocalMove(originalShadowPos, 0.15f).SetEase(Ease.OutQuad);
            }
        }
    }

    public void HandleHitVisuals()
    {
        // Play hit VFX from station
        VFXStation.PlayEffect(HitEffectName, transform.position);
        
        // 1. Kill and Reset to prevent stacking
        visualRoot.DOKill(false); 
        DOTween.Kill(this);
        
        visualRoot.localScale = originalScale;
        visualRoot.localPosition = originalLocalPos;

        // 2. Execute Effects
        FlashWhiteOnHit();
        ScalePop();
        Wiggle();
    }

    public void PlayShake()
    {
        visualRoot.DOKill(false);
        visualRoot.localPosition = originalLocalPos;
        visualRoot.localRotation = originalLocalRotation;
        RotationShake();
    }

    private void HandleDeathVisuals()
    {
        VFXStation.PlayEffect(DeathEffectName, transform.position);
    }

    private void ScalePop()
    {
        visualRoot.DOPunchScale(new Vector3(PunchAmount, PunchAmount, 0), PunchDuration, 10, 0.5f)
                  .SetTarget(visualRoot);
    }

    private void Wiggle()
    {
        // Punching on the X axis creates that "left-right" wiggle effect
        visualRoot.DOPunchPosition(new Vector3(ShakeStrength, 0, 0), ShakeDuration, ShakeVibrato, 1)
                  .SetTarget(visualRoot);
    }

    private void RotationShake()
    {
        visualRoot.DOPunchRotation(ShakeRotationAngle, ShakeDuration, ShakeVibrato, 1)
                  .SetTarget(visualRoot);
    }

    private void FlashWhiteOnHit()
    {
        float flashValue = 0f;
        DOTween.To(() => flashValue, x =>
        {
            flashValue = x;
            SetFlashOnAll(x);
        }, 1f, FlashDuration / 2f)
        .SetTarget(this)
        .OnComplete(() =>
        {
            DOTween.To(() => flashValue, x =>
            {
                flashValue = x;
                SetFlashOnAll(x);
            }, 0f, FlashDuration / 2f)
            .SetTarget(this);
        });
    }

    private void SetFlashOnAll(float value)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr == null) continue;
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(FlashProperty, value);
            sr.SetPropertyBlock(mpb);
        }
    }
}
