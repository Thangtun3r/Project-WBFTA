using UnityEngine;

public class BombOnKillLogic : ProcOnKillItemLogicBase
{
    [Header("Fallback Stats")]
    [SerializeField] private float fallbackProcChance = 0.05f;
    [SerializeField] private float fallbackProcChancePerStack = 0.05f;
    [SerializeField] private float fallbackDamageMultiplier = 2.5f;

    [Header("Projectile")]
    [SerializeField] private string projectileId = "Bomb";

    protected override float ProcChance => GetProcChance(fallbackProcChance, fallbackProcChancePerStack);

    private float DamageMultiplier => GetDamageMultiplier(fallbackDamageMultiplier);

    protected override void ExecuteTrigger(ItemTriggerContext context)
    {
        SpawnBomb(context.Origin, context.Damage, context.IsCrit);
    }

    private void SpawnBomb(Vector3 killPosition, float baseDamage, bool isCrit)
    {
        float finalDamage = baseDamage * DamageMultiplier;

        Vector3 spawnOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, Random.Range(-0.3f, 0.3f));

        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = projectileId,
            Position = killPosition + spawnOffset,
            Rotation = Quaternion.identity,
            Direction = Vector3.zero, // Bombs don't move, they explode on spawn
            Damage = finalDamage,
            Target = null
        };


        ProjectilePool.Instance?.RequestProjectile(request);
    }
}
