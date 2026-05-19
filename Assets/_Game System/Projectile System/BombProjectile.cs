using UnityEngine;
using System;
using DG.Tweening;

public class BombProjectile : MonoBehaviour, IProjectile
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 3f;
     private float explosionDamage;
    [SerializeField] private string explosionVFXName = "explosion"; // Matches VFXStation name
    [SerializeField] private LayerMask damageLayer;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteToTint;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.15f;
    [SerializeField] private int shakeVibrato = 10;

    private bool _hasExploded;
    private Action<IProjectile> _onRelease;
    private Sequence _bombSequence;
    private SpriteRenderer[] spriteRenderers;
    private MaterialPropertyBlock mpb;
    private static readonly int FlashProperty = Shader.PropertyToID("_Flash");
    
    // Pre-allocate array to avoid GC (Garbage Collection) spikes during explosion
    private readonly Collider2D[] _hitResults = new Collider2D[15];



    private void OnEnable()
    {
        // Initialize sprite renderers and property block on first use
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            mpb = new MaterialPropertyBlock();
        }
    }

    public void Launch(ProjectileRequest request, Action<IProjectile> onRelease)
    {
        _onRelease = onRelease;
        _hasExploded = false;
        transform.position = request.Position;
        explosionDamage = request.Damage;

        PlayShakeAndExplode();
    }

    private void PlayShakeAndExplode()
    {
        _bombSequence?.Kill();
        _bombSequence = DOTween.Sequence();

        // Phase 1 (0.0s to 1.4s - First 70%): Slow flashing
        for (int i = 0; i < 2; i++) // 2 cycles of 0.7s = 1.4s
        {
            _bombSequence.AppendCallback(() => SetFlashOnAll(1f));
            _bombSequence.AppendInterval(0.35f);
            _bombSequence.AppendCallback(() => SetFlashOnAll(0f));
            _bombSequence.AppendInterval(0.35f);
        }

        // Phase 2 (1.4s to 2.0s - Last 30%): Fast flashing
        for (int i = 0; i < 3; i++) // 3 cycles of 0.2s = 0.6s
        {
            _bombSequence.AppendCallback(() => SetFlashOnAll(1f));
            _bombSequence.AppendInterval(0.1f);
            _bombSequence.AppendCallback(() => SetFlashOnAll(0f));
            _bombSequence.AppendInterval(0.1f);
        }

        // Phase 1 Shake: Low amplitude, slow vibrato (Starts at 0.0s, lasts 1.4s)
        _bombSequence.Insert(0f, transform.DOPunchPosition(
            new Vector3(shakeMagnitude * 0.3f, shakeMagnitude * 0.3f, 0),
            1.4f, 
            Mathf.Max(1, shakeVibrato / 2), 
            1f));
            
        // Phase 2 Shake: High amplitude, fast vibrato (Starts at 1.4s, lasts 0.6s)
        _bombSequence.Insert(1.4f, transform.DOPunchPosition(
            new Vector3(shakeMagnitude, shakeMagnitude, 0),
            0.6f, 
            shakeVibrato * 2, 
            1f));

        // Trigger explosion when sequence finishes (exactly after 2.0s)
        _bombSequence.OnComplete(Explode);
    }

    private void SetFlashOnAll(float value)
    {
        if (spriteRenderers == null || mpb == null) return;
        
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr == null) continue;
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(FlashProperty, value);
            sr.SetPropertyBlock(mpb);
        }
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // 1. Call your VFXStation (Global Pooler)
        VFXStation.PlayEffect(explosionVFXName, transform.position);
        
        // Trigger generic camera/HUD shake
        OnScreenEffect.Instance?.ShakeHUD();

        // 2. Efficient Physics (Zero Allocation)
        int numColliders = Physics2D.OverlapCircleNonAlloc(
            transform.position, 
            explosionRadius, 
            _hitResults, 
            damageLayer
        );

        // 3. Apply Damage
        for (int i = 0; i < numColliders; i++)
        {
            if (_hitResults[i].TryGetComponent(out IDamagable damagable))
            {
                damagable.TakeDamage(explosionDamage);
            }
        }

        // 4. Return to pool
        Deactivate();
    }

    private void Deactivate()
    {
        _bombSequence?.Kill();
        _onRelease?.Invoke(this);
    }

    private void OnDisable()
    {
        _bombSequence?.Kill();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}