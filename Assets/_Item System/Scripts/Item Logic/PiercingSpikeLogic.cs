using UnityEngine;

public class PiercingSpikeLogic : ProcOnHitItemLogicBase
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackProcChance = 0.2f;
    [SerializeField] private float fallbackDamageMultiplier = 0.2f;
    [SerializeField] private float fallbackDamageMultiplierPerStack = 0.1f;

    [Header("Projectile")]
    [SerializeField] private string projectileId = "PiercingSpikeLauncher";
    [SerializeField] private float launchSpeed = 22f;

    protected override float ProcChance => GetProcChance(fallbackProcChance);

    private float DamageMultiplier => GetDamageMultiplier(
        fallbackDamageMultiplier,
        fallbackDamageMultiplierPerStack);

    protected override void ExecuteTrigger(ItemTriggerContext context)
    {
        Transform targetTransform = context.Target.GetTransform();
        Vector3 spawnPosition = Owner.OwnerObject.transform.position;
        Vector3 direction = (targetTransform.position - spawnPosition).normalized;

        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = projectileId,
            Position = spawnPosition,
            Rotation = Quaternion.identity,
            Direction = direction * launchSpeed,
            Target = targetTransform,
            Damage = context.Damage * DamageMultiplier,
            Speed = launchSpeed
        };

        ProjectilePool.Instance?.RequestProjectile(request);
    }
}
