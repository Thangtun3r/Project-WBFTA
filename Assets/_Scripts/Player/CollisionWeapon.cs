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
    [SerializeField] private float repeatHitInterval = 0.15f;

    [Header("Icon")]
    [SerializeField] private Sprite iconSprite;

    [Header("References")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Collider2D[] hitboxColliders;
    [SerializeField] private GhostSlashSkill ghostSlashSkill;

    private bool _active = true;
    private bool _damageSuspended;
    private float _lastAttackTime = -Mathf.Infinity;
    private readonly Collider2D[] _overlapResults = new Collider2D[32];
    private readonly HashSet<Collider2D> _currentOverlaps = new HashSet<Collider2D>();
    private readonly Dictionary<IDamagable, float> _lastTargetHitTimes = new Dictionary<IDamagable, float>();

    public float DamageMultiplier => damageMultiplier;
    public float ProcCoefficient => procCoefficient;
    public Sprite CurrentSprite => iconSprite;
    public PlayerAttack PlayerAttack => playerAttack;
    public LayerMask TargetLayers => targetLayers;

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

        if (ghostSlashSkill == null)
            ghostSlashSkill = GetComponent<GhostSlashSkill>();
    }

    public void SetWeaponActive(bool active)
    {
        _active = active;

        if (!_active)
            ClearHitState();

        ghostSlashSkill?.SetSkillActive(active);
    }

    public void SetDamageSuspended(bool suspended)
    {
        _damageSuspended = suspended;

        if (_damageSuspended)
            ClearHitState();
    }

    private void FixedUpdate()
    {
        if (!_active || _damageSuspended || detectionMethod != AttackDetectionMethod.Trigger)
            return;

        ProcessTriggerHitboxes();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_active || _damageSuspended || detectionMethod != AttackDetectionMethod.Collision) return;
        if (collision == null || collision.collider == null) return;

        Vector2 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : collision.collider.transform.position;

        HandleHit(collision.collider, hitPoint);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_active || _damageSuspended || detectionMethod != AttackDetectionMethod.Trigger) return;
        if (collision == null) return;

        HandleHit(collision, collision.transform.position);
    }

    private void HandleHit(Collider2D collider, Vector2 hitPoint)
    {
        if (playerAttack == null) return;
        if (targetLayers.value != 0 && ((1 << collider.gameObject.layer) & targetLayers) == 0) return;
        if (Time.time - _lastAttackTime < attackGraceTime) return;

        IDamagable damagable = collider.GetComponent<IDamagable>() ?? collider.GetComponentInParent<IDamagable>();
        if (damagable == null || !CanHitTarget(damagable)) return;

        if (playerAttack.DealDamage(damagable, hitPoint, damageMultiplier, procCoefficient, gameObject))
        {
            _lastAttackTime = Time.time;
            _lastTargetHitTimes[damagable] = Time.unscaledTime;
        }
    }

    private bool CanHitTarget(IDamagable target)
    {
        float interval = Mathf.Max(0f, repeatHitInterval);
        return !_lastTargetHitTimes.TryGetValue(target, out float lastHitTime)
            || Time.unscaledTime >= lastHitTime + interval;
    }

    private void ClearHitState()
    {
        _currentOverlaps.Clear();
        _lastTargetHitTimes.Clear();
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

                if (_currentOverlaps.Add(candidate))
                {
                    HandleHit(candidate, candidate.ClosestPoint(hitbox.bounds.center));
                }
            }
        }

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
