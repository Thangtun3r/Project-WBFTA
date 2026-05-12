using System;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleProjectile : MonoBehaviour, IProjectile
{
    [Header("Black Hole Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float suctionRadius = 4f;
    [SerializeField] private float suctionForce = 30f;
    [SerializeField] private float damageTickInterval = 0.5f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [Header("Playtest")]
    [SerializeField] private bool launchOnEnableForPlaytest = true;
    [SerializeField] private float playtestDamagePerTick = 1f;

    private readonly Collider2D[] _hitResults = new Collider2D[32];
    private readonly Dictionary<int, float> _nextDamageTimeByColliderId = new Dictionary<int, float>();

    private float _damagePerTick;
    private Action<IProjectile> _onRelease;
    private bool _isActive;

    private float _nextTickTime;
    private const float PhysicsTickInterval = 0.1f;

    public void Launch(ProjectileRequest request, Action<IProjectile> onRelease)
    {
        Activate(request.Damage, onRelease);
    }

    private void OnEnable()
    {
        if (launchOnEnableForPlaytest && !_isActive)
        {
            Activate(playtestDamagePerTick, null);
        }
    }

    private void Activate(float damagePerTick, Action<IProjectile> onRelease)
    {
        _damagePerTick = damagePerTick;
        _onRelease = onRelease;
        _isActive = true;
        _nextDamageTimeByColliderId.Clear();

        CancelInvoke();
        Invoke(nameof(Deactivate), lifetime);
    }

    private void FixedUpdate()
    {
        if (!_isActive || Time.time < _nextTickTime)
        {
            return;
        }

        _nextTickTime = Time.time + PhysicsTickInterval;

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            suctionRadius,
            _hitResults,
            targetLayers
        );

        float currentTime = Time.time;
        Vector2 center = transform.position;

        // Multiply force to compensate for running checks less frequently
        float forceMultiplier = PhysicsTickInterval / Time.fixedDeltaTime;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _hitResults[i];
            if (hit == null)
            {
                continue;
            }

            Vector2 toCenter = center - (Vector2)hit.transform.position;
            float distance = Mathf.Max(toCenter.magnitude, 0.01f);
            Vector2 pullDirection = toCenter / distance;

            if (hit.attachedRigidbody != null)
            {
                hit.attachedRigidbody.AddForce(pullDirection * suctionForce * forceMultiplier, ForceMode2D.Force);
            }

            int hitId = hit.GetInstanceID();
            
            // Check dictionary cooldown first to avoid TryGetComponent where possible
            if (!_nextDamageTimeByColliderId.TryGetValue(hitId, out float nextAllowedDamageTime) ||
                currentTime >= nextAllowedDamageTime)
            {
                if (hit.TryGetComponent(out IDamagable damagable))
                {
                    damagable.TakeDamage(_damagePerTick);
                    _nextDamageTimeByColliderId[hitId] = currentTime + damageTickInterval;
                }
                else
                {
                    // Cache that this object expects no damage to avoid repeated TryGetComponent overhead
                    _nextDamageTimeByColliderId[hitId] = currentTime + 9999f;
                }
            }
        }
    }

    private void Deactivate()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _nextDamageTimeByColliderId.Clear();
        if (_onRelease != null)
        {
            _onRelease.Invoke(this);
            return;
        }

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CancelInvoke();
        _isActive = false;
        _nextDamageTimeByColliderId.Clear();
        _onRelease = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, suctionRadius);
    }
}
