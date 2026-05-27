using UnityEngine;
using _Scripts.Enemy;

public class EnemyRewardManager : MonoBehaviour
{
    public static EnemyRewardManager Instance { get; private set; }

    [Tooltip("The 'Base Reward' from your spreadsheet weighted average (e.g., 9)")]
    public int baseReward = 9;

    [Tooltip("Reward coefficient multiplier for this manager (e.g., 1.0 = normal, 1.5 = 50% more rewards)")]
    public float rewardCoefficient = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        BaseEnemy.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        BaseEnemy.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled(BaseEnemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        int tier = enemy.Config != null ? enemy.Config.tier : 1;
        HandleReward(tier, enemy.CurrentLevel);
    }

    /// <summary>
    /// Calculates reward using Tier (Type), Level (Specific Strength), and World Difficulty (Inflation).
    /// </summary>
    /// <param name="enemyTier">1 for Fodder, 4 for Elite, 10 for Miniboss, etc.</param>
    /// <param name="enemyLevel">The specific level of the enemy instance.</param>
    public void HandleReward(int enemyTier, int enemyLevel)
    {
        // 1. Get the Inflation Factor from your GameManager
        float inflation = GameManager.Instance != null ? GameManager.Instance.DifficultyCoefficient : 1f;

        // 2. Combine all three factors:
        // (Base * Tier * Level) = The 'Physical' value of the enemy
        // (* inflation) = Ensures this value stays relevant as chest prices rise
        // (* rewardCoefficient) = Manager-specific reward multiplier
        float rawReward = baseReward * enemyTier * enemyLevel * inflation * rewardCoefficient;
        
        // 3. Add +/- 10% variance for a more natural feel
        float variance = Random.Range(0.9f, 1.1f);
        int finalReward = Mathf.RoundToInt(rawReward * variance);

    
        
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(finalReward);
        }
    }
}
