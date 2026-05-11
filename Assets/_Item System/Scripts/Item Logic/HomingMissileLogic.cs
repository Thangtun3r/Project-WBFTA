using UnityEngine;

public class HomingMissileLogic : ItemLogicBase
{
    [Header("Debug Settings")]
    private bool forceProc = false; // Set this to TRUE in Inspector
    private float procChance = 0.2f;
    private float damageMultiplier = 3f;
    private float damageMultiplierPerStack = 3f;

    protected override void OnInitialize()
    {
        if (GlobalEventManager.Instance != null)
            GlobalEventManager.Instance.HandleOnHit += OnHitEffect;
    }

    private void OnHitEffect(GameObject attacker, IDamagable target, float damage, bool isCrit)
    {
        // DEBUG LOG 1: See if the event is even reaching the item
        // Debug.Log($"Missile Logic: Hit detected. Attacker: {attacker.name}, Owner: {Owner.OwnerObject.name}");

        if (attacker != Owner.OwnerObject) return;

        // DEBUG OVERRIDE: Ignore the Random.value check if forceProc is on
        if (!forceProc && Random.value > procChance) return;

        Transform targetTransform = (target as MonoBehaviour)?.transform;
        
        LaunchMissileSalvo(targetTransform, damage);
    }

    private void LaunchMissileSalvo(Transform target, float baseDamage)
    {
        // Calculate damage multiplier based on stack size
        float scaledDamageMultiplier = damageMultiplier + (Owner.StackSize - 1) * damageMultiplierPerStack;
        
        Vector3 spawnOffset = new Vector3(Random.Range(-0.2f, 0.2f), 1.5f, 0);
        
        // Randomize launch direction so they fan out beautifully
        Vector3 randomDir = (Vector3.up + (Vector3)Random.insideUnitCircle * 0.7f).normalized;

        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = "HomingMissile",
            Position = Owner.OwnerObject.transform.position + spawnOffset,
            Rotation = Quaternion.identity,
            Direction = randomDir * 6f, // Speed of the initial "pop"
            Damage = baseDamage * scaledDamageMultiplier,
            Target = target
        };

        ProjectilePool.Instance?.RequestProjectile(request);
    }

    public override void Dispose()
    {
        if (GlobalEventManager.Instance != null)
            GlobalEventManager.Instance.HandleOnHit -= OnHitEffect;
    }
}