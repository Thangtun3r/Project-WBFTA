using UnityEngine;
using System.Collections.Generic;

public enum AttackDetectionMethod
{
    Collision,
    Trigger
}

public class CollisionWeapon : MonoBehaviour, IPlayerWeapon
{
    [Header("Detection Settings")]
    [SerializeField] private AttackDetectionMethod detectionMethod = AttackDetectionMethod.Collision;
    [SerializeField] private LayerMask targetLayers;

    [Header("Weapon Tuning")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float procCoefficient = 1f;
    [SerializeField] private float attackGraceTime = 0.005f;

    [Header("Icon")]
    [SerializeField] private Sprite iconSprite;

    [Header("References")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Collider2D[] hitboxColliders;

    private bool _active = true;
    private float _lastAttackTime = -Mathf.Infinity;
    private readonly Collider2D[] _overlapResults = new Collider2D[32];
    private readonly HashSet<Collider2D> _previousOverlaps = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> _currentOverlaps = new HashSet<Collider2D>();

    public float DamageMultiplier => damageMultiplier;
    public float ProcCoefficient => procCoefficient;
    public Sprite CurrentSprite => iconSprite;

    private void Awake()
    {
        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>()
                ?? GetComponentInChildren<PlayerAttack>()
                ?? GetComponentInParent<PlayerAttack>()
                ?? FindFirstObjectByType<PlayerAttack>();
        }

        if (hitboxColliders == null || hitboxColliders.Length == 0)
            hitboxColliders = ResolveHitboxColliders();
    }

    public void SetWeaponActive(bool active)
    {
        _active = active;

        if (!_active)
        {
            _previousOverlaps.Clear();
            _currentOverlaps.Clear();
        }
    }

    private void FixedUpdate()
    {
        if (!_active || detectionMethod != AttackDetectionMethod.Trigger)
            return;

        ProcessTriggerHitboxes();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_active || detectionMethod != AttackDetectionMethod.Collision) return;
        if (collision == null || collision.collider == null) return;

        Vector2 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : collision.collider.transform.position;

        HandleHit(collision.collider, hitPoint);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_active || detectionMethod != AttackDetectionMethod.Trigger) return;
        if (collision == null) return;

        HandleHit(collision, collision.transform.position);
    }

    private void HandleHit(Collider2D collider, Vector2 hitPoint)
    {
        if (playerAttack == null) return;
        if (targetLayers.value != 0 && ((1 << collider.gameObject.layer) & targetLayers) == 0) return;
        if (Time.time - _lastAttackTime < attackGraceTime) return;

        IDamagable damagable = collider.GetComponent<IDamagable>() ?? collider.GetComponentInParent<IDamagable>();
        if (playerAttack.DealDamage(damagable, hitPoint, damageMultiplier, procCoefficient, gameObject))
        {
            _lastAttackTime = Time.time;
        }
    }

    private void ProcessTriggerHitboxes()
    {
        _currentOverlaps.Clear();

        if (hitboxColliders == null)
            return;

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true
        };

        if (targetLayers.value != 0)
            filter.SetLayerMask(targetLayers);
        else
            filter.NoFilter();

        for (int i = 0; i < hitboxColliders.Length; i++)
        {
            Collider2D hitbox = hitboxColliders[i];
            if (hitbox == null || !hitbox.enabled)
                continue;

            int hitCount = hitbox.Overlap(filter, _overlapResults);
            for (int h = 0; h < hitCount; h++)
            {
                Collider2D candidate = _overlapResults[h];
                if (candidate == null || candidate == hitbox)
                    continue;

                if (_currentOverlaps.Add(candidate) && !_previousOverlaps.Contains(candidate))
                {
                    HandleHit(candidate, candidate.ClosestPoint(hitbox.bounds.center));
                }
            }
        }

        _previousOverlaps.Clear();
        foreach (Collider2D collider in _currentOverlaps)
            _previousOverlaps.Add(collider);
    }

    private Collider2D[] ResolveHitboxColliders()
    {
        List<Collider2D> colliders = new List<Collider2D>();
        AddColliders(GetComponents<Collider2D>(), colliders);
        AddColliders(GetComponentsInChildren<Collider2D>(), colliders);

        if (playerAttack != null)
            AddColliders(playerAttack.GetComponents<Collider2D>(), colliders);

        return colliders.ToArray();
    }

    private static void AddColliders(Collider2D[] source, List<Collider2D> destination)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && !destination.Contains(source[i]))
                destination.Add(source[i]);
        }
    }
}
