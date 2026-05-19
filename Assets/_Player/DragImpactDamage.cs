using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class DragImpactDamage : MonoBehaviour
{
    private bool logImpact = true;
    private bool drawImpactRay = true;
    private Color impactColor = Color.red;
    private float debugLineDuration = 0.5f;

    [SerializeField] private string impactEffectName = "impactParticle";
    private float impactVelocityThreshold = 1f;
    private float impactGraceTime = 0.1f;
    private float thrownStateFalloffVelocity = 1f;
    
    private float _lastImpactTime = -Mathf.Infinity;
    private float _playerThrownDamage = 0f;
    private bool _isCrit = false;

    public static event Action OnImpactDetected;

    private Rigidbody2D _rb;
    private EffectManager _effectManager;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _effectManager = GetComponent<EffectManager>();
    }
    
    public void SetPlayerThrowSource(float damageOutput, bool isCrit)
    {
        _playerThrownDamage = Mathf.Max(0f, damageOutput);
        _isCrit = isCrit;
    }

    private void Update()
    {
        if (_effectManager != null && _effectManager.WasThrown && _rb.linearVelocity.magnitude <= thrownStateFalloffVelocity)
        {
            _effectManager.WasThrown = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time < _lastImpactTime + impactGraceTime)
            return;

        float impactSpeed = Mathf.Max(collision.relativeVelocity.magnitude, _rb.linearVelocity.magnitude);
        if (impactSpeed < impactVelocityThreshold)
            return;

        _lastImpactTime = Time.time;
        OnImpactDetected?.Invoke();
        PlayImpactEffect(collision);

        if (logImpact)
        {
            Debug.Log($"Impact Detected! Object: {collision.gameObject.name}, Speed: {impactSpeed}");
        }

        IDamagable damagable = collision.gameObject.GetComponent<IDamagable>()
            ?? collision.gameObject.GetComponentInParent<IDamagable>();

        if (damagable == null)
        {
            damagable = GetComponent<IDamagable>() ?? GetComponentInParent<IDamagable>();
        }

        float damageToApply = GetCursorDamageOutput(collision, out bool isPlayerOwnedHit, out bool isCrit);
        if (damagable != null && damageToApply > 0f)
        {
            damagable.TakeDamage(damageToApply);

            if (isPlayerOwnedHit)
            {
                // DragImpactDamage is the single damage source; this only tags the hit as player-owned.
                GlobalEventManager.Instance?.OnPlayerHit(damagable, damageToApply, isCrit);
            }

            // Spawn floating damage text at impact point
            FloatingDamagePool.Instance?.SpawnDamage(collision.contacts[0].point, damageToApply, isCrit);
        }
    }

    private float GetCursorDamageOutput(Collision2D collision, out bool isPlayerOwnedHit, out bool isCrit)
    {
        isPlayerOwnedHit = false;
        isCrit = false;

        if (_effectManager != null && _effectManager.WasThrown && _playerThrownDamage > 0f)
        {
            isPlayerOwnedHit = true;
            isCrit = _isCrit;
            return _playerThrownDamage;
        }

        DragImpactDamage otherImpactDamage = GetOtherImpactDamage(collision);

        if (otherImpactDamage != null &&
            otherImpactDamage._effectManager != null &&
            otherImpactDamage._effectManager.WasThrown &&
            otherImpactDamage._playerThrownDamage > 0f)
        {
            isPlayerOwnedHit = true;
            isCrit = otherImpactDamage._isCrit;
            return otherImpactDamage._playerThrownDamage;
        }

        return 0f;
    }

    private void PlayImpactEffect(Collision2D collision)
    {
        if (string.IsNullOrWhiteSpace(impactEffectName))
        {
            return;
        }

        DragImpactDamage otherImpactDamage = GetOtherImpactDamage(collision);
        if (otherImpactDamage != null && GetInstanceID() > otherImpactDamage.GetInstanceID())
        {
            return;
        }

        Vector3 effectPosition = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;

        VFXStation.PlayEffect(impactEffectName, effectPosition);
    }

    private DragImpactDamage GetOtherImpactDamage(Collision2D collision)
    {
        return collision.rigidbody != null
            ? collision.rigidbody.GetComponent<DragImpactDamage>()
            : collision.gameObject.GetComponentInParent<DragImpactDamage>();
    }
}
