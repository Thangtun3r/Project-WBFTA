using UnityEngine;
using System;

public class MissileProjectile : MonoBehaviour, IProjectile
{
    [Header("Movement Settings")]
    private float speed = 22f;
    private float rotateSpeed = 1000f;
    private float searchRadius = 35f;
    
    [Header("Safety Settings")]
    private float safetyRadius = 0f; 
    private float graceTime = 0.3f; // Time in seconds before missile can explode

    private Rigidbody2D _rb;
    private Transform _target;
    private float _damage;
    private Action<IProjectile> _returnToPool;
    private bool _isActive;
    private Vector3 _spawnPosition; 
    private float _armedTime; // Timestamp of when the missile becomes "deadly"

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
        _isActive = true;
        _damage = request.Damage;
        _returnToPool = onRelease;
        _spawnPosition = request.Position;
        _armedTime = Time.time + graceTime; 

        // --- ADD THIS LINE TO RANDOMIZE STARTING ANGLE ---
        transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360f));

        _target = request.Target;
        if (_target == null) FindNewTarget();

        if (_rb != null)
        {
            _rb.angularVelocity = 0f;
            // Use the new random rotation to set the initial velocity direction
            _rb.linearVelocity = transform.right * speed; 
        }

        CancelInvoke();
        Invoke(nameof(Deactivate), 5f);
    }

    private void FixedUpdate()
    {
        if (!_isActive) return;

        // --- NEW: SKIP TRACKING DURING GRACE TIME ---
        if (Time.time < _armedTime)
        {
            // During grace time, just move forward in whatever direction it was launched
            _rb.angularVelocity = 0f;
            _rb.linearVelocity = transform.right * speed;
            return; 
        }

        // --- EXISTING TRACKING LOGIC ---
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            _target = null;
            if (Time.frameCount % 10 == 0) FindNewTarget();

            _rb.angularVelocity = 0f;
            _rb.linearVelocity = transform.right * speed;
            return;
        }

        Vector2 direction = (Vector2)_target.position - _rb.position;
        direction.Normalize();
        float rotateAmount = Vector3.Cross(direction, transform.right).z;

        _rb.angularVelocity = -rotateAmount * rotateSpeed;
        _rb.linearVelocity = transform.right * speed;
    }

    private void FindNewTarget()
    {
        if (CurrentEnemyRegistry.Instance == null) return;

        var potentialTargets = CurrentEnemyRegistry.Instance.GetTargetsInRadius(transform.position, searchRadius, 1);
        
        if (potentialTargets.Count > 0)
        {
            _target = potentialTargets[0].transform;
        }
    }

    private void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;
        _returnToPool?.Invoke(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isActive) return;

        // 1. GRACE TIME CHECK (Arming Delay)
        if (Time.time < _armedTime)
        {
            return; // Too young to explode
        }

        // 2. SAFETY RADIUS CHECK (Distance Squared)
        float distSqr = (transform.position - _spawnPosition).sqrMagnitude;
        if (distSqr < (safetyRadius * safetyRadius)) 
        {
            return; // Too close to home to explode
        }

        if (collision.TryGetComponent(out IDamagable target))
        {
            target.TakeDamage(_damage);
            Deactivate();
        }
    }
}
