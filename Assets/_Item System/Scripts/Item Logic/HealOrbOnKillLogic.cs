
using System.Diagnostics;
using _Scripts.Enemy;
public class HealOrbOnKillLogic : ItemLogicBase
{
    private float spawnChance = 0.9f; // 90% chance to spawn a heal orb on kill
        private float healAmount = 2f; //heal amount as a percentage of max health

    private void HandleEnemyDeath(BaseEnemy enemy)
    {
        if (UnityEngine.Random.value < spawnChance)
        {
        
            float calculatedHeal = healAmount * Owner.StackSize; 
            WorldObjectSpawner.Instance.Spawn("HealOrb", enemy.transform.position, calculatedHeal);
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