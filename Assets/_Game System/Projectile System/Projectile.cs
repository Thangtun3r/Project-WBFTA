using UnityEngine;
using System;

public class Projectile : MonoBehaviour, IProjectile
{
    private float _damage;
    private Rigidbody2D _rb;
    private Action<IProjectile> _returnToPool;
    
    private bool _isReleased; // Critical safety flag
    private Vector3 _spawnPosition;
    private float _armedTime;

    [Header("Safety Settings")]
    [SerializeField] private float safetyRadius = 1.5f;
    [SerializeField] private float graceTime = 0.05f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(ProjectileRequest request, Action<IProjectile> onRelease)
    {
        _isReleased = false; // Reset state
        _damage = request.Damage;
        _returnToPool = onRelease;
        _spawnPosition = request.Position;
        _armedTime = Time.time + graceTime;

        // Apply velocity from request
        if (_rb != null)
        {
            _rb.linearVelocity = request.Direction;
        }

        // Auto-return to pool after 5 seconds if it hits nothing
        CancelInvoke(); 
        Invoke(nameof(Deactivate), 5f);
    }

    private void Deactivate()
    {
        if (_isReleased) return; // Prevent double-release crash

        _isReleased = true;
        _returnToPool?.Invoke(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isReleased) return;

        // Safety Checks
        if (Time.time < _armedTime) return;
        if ((transform.position - _spawnPosition).sqrMagnitude < (safetyRadius * safetyRadius)) return;

        if (collision.TryGetComponent(out IDamagable target))
        {
            target.TakeDamage(_damage);
            Deactivate();
        }
    }
}