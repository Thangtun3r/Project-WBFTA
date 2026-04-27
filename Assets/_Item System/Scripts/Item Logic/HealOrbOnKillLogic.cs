
using _Scripts.Enemy;
public class HealOrbOnKillLogic : ItemLogicBase
{
    private float spawnChance = 0.9f; // 90% chance to spawn a heal orb on kill
    private float healAmount = 0.1f; //heal amount as a percentage of max health




    private void HandleEnemyDeath(BaseEnemy enemy)
    {
        if (UnityEngine.Random.value < spawnChance)
        {
            UnityEngine.Debug.Log($"Spawning Heal Orb at {enemy.transform.position} with heal amount {healAmount * 100}% of max health.");
            WorldObjectSpawner.Instance.Spawn("HealOrb", enemy.transform.position);
        }
    }

    public override void Dispose()
    {
        BaseEnemy.OnEnemyDeath -= HandleEnemyDeath;
    }

    protected override void OnInitialize()
    {
        BaseEnemy.OnEnemyDeath += HandleEnemyDeath;
    }
}