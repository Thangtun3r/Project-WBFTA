using UnityEngine;

public class HomingMissileLogic : ProcOnHitItemLogicBase
{
    private const string LaunchSpeedParameterKey = "HomingMissile.LaunchSpeed";

    [Header("Debug Settings")]
    [SerializeField] private bool forceProc;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackProcChance = 0.2f;
    [SerializeField] private float fallbackDamageMultiplier = 3f;
    [SerializeField] private float fallbackDamageMultiplierPerStack = 3f;

    [Header("Projectile")]
    [SerializeField] private string projectileId = "HomingMissile";
    [SerializeField] private float fallbackLaunchSpeed = 6f;

    protected override float ProcChance => GetProcChance(fallbackProcChance);

    private float DamageMultiplier => GetDamageMultiplier(
        fallbackDamageMultiplier,
        fallbackDamageMultiplierPerStack);

    private float LaunchSpeed => GetParameter(LaunchSpeedParameterKey, fallbackLaunchSpeed);

    protected override bool RollProc(ItemTriggerContext context)
    {
        return forceProc || base.RollProc(context);
    }

    protected override void ExecuteTrigger(ItemTriggerContext context)
    {
        Transform targetTransform = context.Target.GetTransform();
        LaunchMissileSalvo(targetTransform, context.Damage);
    }

    private void LaunchMissileSalvo(Transform target, float baseDamage)
    {
        Vector3 spawnOffset = new Vector3(Random.Range(-0.2f, 0.2f), 1.5f, 0);
        
        // Randomize launch direction so they fan out beautifully
        Vector3 randomDir = (Vector3.up + (Vector3)Random.insideUnitCircle * 0.7f).normalized;

        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = projectileId,
            Position = Owner.OwnerObject.transform.position + spawnOffset,
            Rotation = Quaternion.identity,
            Direction = randomDir * LaunchSpeed,
            Damage = baseDamage * DamageMultiplier,
            Target = target
        };

        ProjectilePool.Instance?.RequestProjectile(request);
    }
}
