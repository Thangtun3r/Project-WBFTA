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
    private PlayerAttack _playerAttack;
    private GameObject _playerHitSource;
    private float _damageMultiplier = 1f;
    private float _procCoefficient = 1f;

    public static event Action OnImpactDetected;

    private Rigidbody2D _rb;
    private EffectManager _effectManager;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _effectManager = GetComponent<EffectManager>();
    }
    
    public void SetPlayerThrowSource(PlayerAttack playerAttack, float damageMultiplier, float procCoefficient, GameObject source)
    {
        _playerAttack = playerAttack;
        _damageMultiplier = Mathf.Max(0f, damageMultiplier);
        _procCoefficient = Mathf.Max(0f, procCoefficient);
        _playerHitSource = source;
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
        
        // Shake the assigned HUD/object:
        OnScreenEffect.Instance?.ShakeHUD();
        OnScreenEffect.Instance?.PlayImpactFrame();

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

        DragImpactDamage damageSource = GetPlayerOwnedDamageSource(collision);
        if (damagable != null && damageSource != null && damageSource._playerAttack != null)
        {
            Vector2 hitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : (Vector2)collision.transform.position;

            damageSource._playerAttack.DealDamage(
                damagable,
                hitPoint,
                damageSource._damageMultiplier,
                damageSource._procCoefficient,
                damageSource._playerHitSource != null ? damageSource._playerHitSource : damageSource.gameObject);
        }
    }

    private DragImpactDamage GetPlayerOwnedDamageSource(Collision2D collision)
    {
        if (_effectManager != null && _effectManager.WasThrown && _playerAttack != null)
        {
            return this;
        }

        DragImpactDamage otherImpactDamage = GetOtherImpactDamage(collision);
        if (otherImpactDamage != null &&
            otherImpactDamage._effectManager != null &&
            otherImpactDamage._effectManager.WasThrown &&
            otherImpactDamage._playerAttack != null)
        {
            return otherImpactDamage;
        }

        return null;
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
