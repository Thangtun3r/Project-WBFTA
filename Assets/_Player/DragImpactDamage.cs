using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class DragImpactDamage : MonoBehaviour
{
    private bool logImpact = true;
    private bool drawImpactRay = true;
    private Color impactColor = Color.red;
    private float debugLineDuration = 0.5f;

    private float damageFalloffVelocity = 1f;
    private float impactGraceTime = 0.1f;
    
    private float _lastImpactTime = -Mathf.Infinity;
    private float _currentDamage = 0f;
    private bool _isCrit = false;

    public static event Action OnImpactDetected;

    private Rigidbody2D _rb;
    private EffectManager _effectManager;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _effectManager = GetComponent<EffectManager>();
    }
    
    public void SetDamage(float damage)
    {
        _currentDamage = damage;
    }
    
    public void SetCrit(bool isCrit)
    {
        _isCrit = isCrit;
    }

    private void Update()
    {
        if (_effectManager != null && _effectManager.WasThrown && _rb.linearVelocity.magnitude <= damageFalloffVelocity)
        {
            _effectManager.WasThrown = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check for grace period
        if (Time.time < _lastImpactTime + impactGraceTime)
            return;

        // Double check this object is currently flagged as "thrown" by the player
        if (_effectManager != null && !_effectManager.WasThrown)
            return;

        // Check if the collision object is in the specified layer
        if (((1 << collision.gameObject.layer) & -1) != 0)
        {
            _lastImpactTime = Time.time;
            OnImpactDetected?.Invoke();

            if (logImpact)
            {
                Debug.Log($"Impact Detected! Object: {collision.gameObject.name}");
            }

            // Deal damage to the object we hit
            IDamagable damagable = collision.gameObject.GetComponent<IDamagable>();
            if (damagable != null && _currentDamage > 0)
            {
                damagable.TakeDamage(_currentDamage);
                
                // Trigger item on-hit effects
                GlobalEventManager.Instance?.OnHit(gameObject, damagable, _currentDamage, _isCrit);

                // Spawn floating damage text at impact point
                FloatingDamagePool.Instance?.SpawnDamage(collision.contacts[0].point, _currentDamage, _isCrit);
            }
            }
        }
    }
