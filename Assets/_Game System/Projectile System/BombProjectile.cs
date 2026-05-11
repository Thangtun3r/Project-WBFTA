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

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.15f;
    [SerializeField] private int shakeVibrato = 10;

    private bool _hasExploded;
    private Action<IProjectile> _onRelease;
    private Sequence _bombSequence;
    
    // Pre-allocate array to avoid GC (Garbage Collection) spikes during explosion
    private readonly Collider2D[] _hitResults = new Collider2D[15];



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

        // 1. Shake effect (The "Delay" happens during the shake duration)
        _bombSequence.Append(transform.DOPunchPosition(
            new Vector3(shakeMagnitude, shakeMagnitude, 0),
            shakeDuration, 
            shakeVibrato, 
            2f));

        // 2. Trigger explosion immediately when shake finishes
        _bombSequence.OnComplete(Explode);
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // 1. Call your VFXStation (Global Pooler)
        VFXStation.PlayEffect(explosionVFXName, transform.position);

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