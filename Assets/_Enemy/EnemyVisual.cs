using UnityEngine;
using DG.Tweening;
using _Scripts.Enemy;

public class EnemyVisual : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The object that will scale and shake (e.g., the 'GFX' child)")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] public SpriteRenderer[] spriteRenderers;

    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Scale Pop Settings")]
    private float punchAmount = 0.25f;
    private float punchDuration = 0.1f;

    [Header("Wiggle Settings")]
    [SerializeField] private float shakeStrength = 0.1f;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private int shakeVibrato = 10;

    [Header("Death VFX")]
    [SerializeField] private string deathEffectName = "enemyDeath";
    [SerializeField] private string hitEffectName = "enemyHit";

    private BaseEnemy parentEnemy;
    private MaterialPropertyBlock mpb;
    private Vector3 originalScale;
    private Vector3 originalLocalPos; // New: Cache for wiggle
    private static readonly int FlashProperty = Shader.PropertyToID("_Flash");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        parentEnemy = GetComponentInParent<BaseEnemy>();

        if (visualRoot == null) visualRoot = transform;
        
        // Cache initial transforms
        originalScale = visualRoot.localScale;
        originalLocalPos = visualRoot.localPosition;

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
        SetFlashOnAll(0f);
    }

    public void HandleHitVisuals()
    {
        // Play hit VFX from station
        VFXStation.PlayEffect(hitEffectName, transform.position);
        
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

    private void HandleDeathVisuals()
    {
        VFXStation.PlayEffect(deathEffectName, transform.position);
    }

    private void ScalePop()
    {
        visualRoot.DOPunchScale(new Vector3(punchAmount, punchAmount, 0), punchDuration, 10, 1)
                  .SetTarget(visualRoot);
    }

    private void Wiggle()
    {
        // Punching on the X axis creates that "left-right" wiggle effect
        visualRoot.DOPunchPosition(new Vector3(shakeStrength, 0, 0), shakeDuration, shakeVibrato, 1)
                  .SetTarget(visualRoot);
    }

    private void FlashWhiteOnHit()
    {
        float flashValue = 0f;
        DOTween.To(() => flashValue, x =>
        {
            flashValue = x;
            SetFlashOnAll(x);
        }, 1f, flashDuration / 2f)
        .SetTarget(this)
        .OnComplete(() =>
        {
            DOTween.To(() => flashValue, x =>
            {
                flashValue = x;
                SetFlashOnAll(x);
            }, 0f, flashDuration / 2f)
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