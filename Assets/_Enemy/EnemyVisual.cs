using UnityEngine;
using DG.Tweening;
using _Scripts.Enemy;

public class EnemyVisual : MonoBehaviour
{
    [SerializeField] public SpriteRenderer[] spriteRenderers;
    [SerializeField] private float flashDuration = 0.1f;
    
    private BaseEnemy parentEnemy;
    private MaterialPropertyBlock mpb;
    private static readonly int FlashProperty = Shader.PropertyToID("_Flash");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        parentEnemy = GetComponentInParent<BaseEnemy>();
        
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (parentEnemy != null)
            parentEnemy.OnEnemyHit += FlashWhiteOnHit;
    }

    private void OnDisable()
    {
        if (parentEnemy != null)
            parentEnemy.OnEnemyHit -= FlashWhiteOnHit;

        DOTween.Kill(this);
        SetFlashOnAll(0f);
    }

        public void FlashWhiteOnHit()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            return;

        DOTween.Kill(this);

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