using UnityEngine;
using _Scripts.Enemy;

public class BombOnKillLogic : ItemLogicBase
{
    [Header("Bomb Settings")]
    private float baseProcChance = 0.05f; // Base 30% chance to spawn bomb on kill
    private float damageMultiplier = 2.5f;

    protected override void OnInitialize()
    {
        if (GlobalEventManager.Instance != null)
        {
            GlobalEventManager.Instance.OnEnemyKilledWithStats += OnEnemyKilled;
        }
    }

    protected override void HandleStackChanged(int amountChanged)
    {
    }

    private void OnEnemyKilled(_Scripts.Enemy.BaseEnemy enemy, float baseDamage, bool isCrit)
    {
        float effectiveProcChance = baseProcChance + (Owner.StackSize - 1) * 0.05f;
        effectiveProcChance = Mathf.Clamp01(effectiveProcChance);

        if (Random.value > effectiveProcChance)
        {
            return;
        }

        SpawnBomb(enemy.transform.position, baseDamage, isCrit);
    }

    private void SpawnBomb(Vector3 killPosition, float baseDamage, bool isCrit)
    {
        float scaledDamageMultiplier = damageMultiplier;
        float finalDamage = baseDamage * scaledDamageMultiplier;

        Vector3 spawnOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, Random.Range(-0.3f, 0.3f));

        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = "Bomb",
            Position = killPosition + spawnOffset,
            Rotation = Quaternion.identity,
            Direction = Vector3.zero, // Bombs don't move, they explode on spawn
            Damage = finalDamage,
            Target = null
        };

    
        ProjectilePool.Instance?.RequestProjectile(request);
    }

    public override void Dispose()
    {
        if (GlobalEventManager.Instance != null)
            GlobalEventManager.Instance.OnEnemyKilledWithStats -= OnEnemyKilled;
    }
}
