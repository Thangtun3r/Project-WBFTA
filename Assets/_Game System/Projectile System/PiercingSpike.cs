using System;
using System.Collections.Generic;
using UnityEngine;

public class PiercingSpike : MonoBehaviour, IProjectile
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 1.5f;

    private readonly HashSet<int> _hitTargets = new HashSet<int>();

    private Rigidbody2D _rb;
    private Action<IProjectile> _onRelease;
    private float _damage;
    private bool _isActive;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    public void Launch(ProjectileRequest request, Action<IProjectile> onRelease)
    {
        _onRelease = onRelease;
        _damage = request.Damage;
        _isActive = true;
        _hitTargets.Clear();

        float effectiveSpeed = request.Speed > 0f ? request.Speed : speed;
        float effectiveLifetime = request.Lifetime > 0f ? request.Lifetime : lifetime;
        Vector2 launchVelocity = request.Direction.sqrMagnitude > 0f
            ? (Vector2)request.Direction.normalized * effectiveSpeed
            : Vector2.right * effectiveSpeed;

        if (_rb != null)
        {
            _rb.linearVelocity = launchVelocity;
        }

        if (launchVelocity.sqrMagnitude > 0f)
        {
            float angle = Mathf.Atan2(launchVelocity.y, launchVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        CancelInvoke();
        Invoke(nameof(Deactivate), effectiveLifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isActive)
        {
            return;
        }

        if (!collision.TryGetComponent(out IDamagable target))
        {
            return;
        }

        int targetId = target.GetTransform().GetInstanceID();
        if (!_hitTargets.Add(targetId))
        {
            return;
        }

        target.TakeDamage(_damage);
    }

    private void Deactivate()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }

        _onRelease?.Invoke(this);
    }

    private void OnDisable()
    {
        CancelInvoke();
        _isActive = false;
        _hitTargets.Clear();

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }

        _onRelease = null;
    }
}
