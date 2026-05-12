using _Scripts.Enemy;
using UnityEngine;

public class BlackHoleLogic : ItemLogicBase
{
    [Header("Proc Settings")]
    [SerializeField] private float procChance = 0.9f;
    private float cooldown = 7f;

    [Header("Projectile Settings")]
    [SerializeField] private string projectileId = "BlackHole";
    [SerializeField] private float damageMultiplier = 0.2f;
    [SerializeField] private float damageMultiplierPerStack = 0.2f;

    private float _nextAllowedTriggerTime;

    protected override void OnInitialize()
    {
        if (GlobalEventManager.Instance != null)
        {
            GlobalEventManager.Instance.OnEnemyKilledWithStats += HandleEnemyKilled;
        }
    }

    protected override void HandleStackChanged(int amountChanged)
    {
    }

    private void HandleEnemyKilled(BaseEnemy enemy, float baseDamage, bool isCrit)
    {
        if (Time.time < _nextAllowedTriggerTime)
        {
            return;
        }

        float effectiveProcChance = Mathf.Clamp01(procChance);
        if (Random.value > effectiveProcChance)
        {
            return;
        }

        _nextAllowedTriggerTime = Time.time + cooldown;

        float effectiveDamageMultiplier = damageMultiplier + (Owner.StackSize - 1) * damageMultiplierPerStack;

        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = projectileId,
            Position = enemy.transform.position,
            Rotation = Quaternion.identity,
            Direction = Vector3.zero,
            Target = null,
            Damage = baseDamage * effectiveDamageMultiplier
        };

        ProjectilePool.Instance?.RequestProjectile(request);
    }

    public override void Dispose()
    {
        if (GlobalEventManager.Instance != null)
        {
            GlobalEventManager.Instance.OnEnemyKilledWithStats -= HandleEnemyKilled;
        }
    }
}
