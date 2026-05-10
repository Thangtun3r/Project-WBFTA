using UnityEngine;

public class HomingMissileLogic : ItemLogicBase
{
    [Header("Debug Settings")]
    [SerializeField] private bool forceProc = true; // Set this to TRUE in Inspector
    [SerializeField] private float procChance = 0.1f;
    [SerializeField] private float damageMultiplier = 0.8f;

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
        
        // DEBUG LOG 2: Confirming we passed the checks
        Debug.Log($"<color=green>MISSILE PROC!</color> Launching {Owner.StackSize} missiles.");

        LaunchMissileSalvo(targetTransform, damage);
    }

    private void LaunchMissileSalvo(Transform target, float baseDamage)
    {
        // Every stack now fires a separate missile
        for (int i = 0; i < Owner.StackSize; i++)
        {
            Vector3 spawnOffset = CalculateSpawnOffset(i);
            
            // Randomize launch direction so they fan out beautifully
            Vector3 randomDir = (Vector3.up + (Vector3)Random.insideUnitCircle * 0.7f).normalized;

            ProjectileRequest request = new ProjectileRequest
            {
                ProjectileID = "HomingMissile",
                Position = Owner.OwnerObject.transform.position + spawnOffset,
                Rotation = Quaternion.identity,
                Direction = randomDir * 6f, // Speed of the initial "pop"
                Damage = baseDamage * damageMultiplier,
                Target = target
            };

            ProjectilePool.Instance?.RequestProjectile(request);
        }
    }

    private Vector3 CalculateSpawnOffset(int index)
    {
        // Spawns them in a vertical line above the player
        return new Vector3(Random.Range(-0.2f, 0.2f), 1.5f + (index * 0.1f), 0);
    }

    public override void Dispose()
    {
        if (GlobalEventManager.Instance != null)
            GlobalEventManager.Instance.HandleOnHit -= OnHitEffect;
    }
}