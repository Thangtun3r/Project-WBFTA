using UnityEngine;

public class BlackHoleLogic : ProcOnKillItemLogicBase
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackProcChance = 0.9f;
    [SerializeField] private float fallbackCooldown = 7f;
    [SerializeField] private float fallbackDamageMultiplier = 0.05f;
    [SerializeField] private float fallbackDamageMultiplierPerStack = 0.05f;

    [Header("Projectile")]
    [SerializeField] private string projectileId = "BlackHole";

    protected override float ProcChance => GetProcChance(fallbackProcChance);
    protected override float Cooldown => GetCooldown(fallbackCooldown);

    private float DamageMultiplier => GetDamageMultiplier(
        fallbackDamageMultiplier,
        fallbackDamageMultiplierPerStack);

    protected override void ExecuteTrigger(ItemTriggerContext context)
    {
        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = projectileId,
            Position = context.Origin,
            Rotation = Quaternion.identity,
            Direction = Vector3.zero,
            Target = null,
            Damage = context.Damage * DamageMultiplier
        };

        ProjectilePool.Instance?.RequestProjectile(request);
    }
}
