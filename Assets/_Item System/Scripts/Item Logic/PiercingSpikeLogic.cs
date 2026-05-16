
using UnityEngine;

public class PiercingSpikeLogic : ItemLogicBase
{
    [Header("Proc Settings")]
    [SerializeField] private float procChance = 0.2f;
    private float cooldown = 0f;

    [Header("Projectile Settings")]
    [SerializeField] private string projectileId = "PiercingSpikeLauncher";
    [SerializeField] private float damageMultiplier = 0.2f;
    [SerializeField] private float damageMultiplierPerStack = 0.1f;
    private float launchSpeed = 22f;

    private float _nextAllowedProcTime;

    protected override void OnInitialize()
    {
        if (GlobalEventManager.Instance != null)
        {
            GlobalEventManager.Instance.HandleOnHit += HandleHit;
        }
    }

    protected override void HandleStackChanged(int amountChanged)
    {
    }

    private void HandleHit(GameObject attacker, IDamagable target, float damage, bool isCrit)
    {
        if (attacker != Owner.OwnerObject || target == null)
        {
            return;
        }

        if (Time.time < _nextAllowedProcTime)
        {
            return;
        }

        if (Random.value > Mathf.Clamp01(procChance))
        {
            return;
        }

        _nextAllowedProcTime = Time.time + cooldown;

        Transform targetTransform = target.GetTransform();
        Vector3 spawnPosition = Owner.OwnerObject.transform.position;
        Vector3 direction = (targetTransform.position - spawnPosition).normalized;
        float effectiveDamageMultiplier = damageMultiplier + ((Owner.StackSize - 1) * damageMultiplierPerStack);

        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = projectileId,
            Position = spawnPosition,
            Rotation = Quaternion.identity,
            Direction = direction * launchSpeed,
            Target = targetTransform,
            Damage = damage * effectiveDamageMultiplier,
            Speed = launchSpeed
        };

        ProjectilePool.Instance?.RequestProjectile(request);
    }

    public override void Dispose()
    {
        if (GlobalEventManager.Instance != null)
        {
            GlobalEventManager.Instance.HandleOnHit -= HandleHit;
        }
    }
}
