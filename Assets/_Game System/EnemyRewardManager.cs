using UnityEngine;

public class EnemyRewardManager : MonoBehaviour
{
    public static EnemyRewardManager Instance { get; private set; }

    [Tooltip("The 'Base Reward' from your spreadsheet weighted average (e.g., 9)")]
    public int baseReward = 9;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Calculates reward using Tier (Type), Level (Specific Strength), and World Difficulty (Inflation).
    /// </summary>
    /// <param name="enemyTier">1 for Fodder, 4 for Elite, 10 for Miniboss, etc.</param>
    /// <param name="enemyLevel">The specific level of the enemy instance.</param>
    public void HandleReward(int enemyTier, int enemyLevel)
    {
        // 1. Get the Inflation Factor from your GameManager
        float inflation = GameManager.Instance.DifficultyCoefficient;

        // 2. Combine all three factors:
        // (Base * Tier * Level) = The 'Physical' value of the enemy
        // (* inflation) = Ensures this value stays relevant as chest prices rise
        float rawReward = baseReward * enemyTier * enemyLevel * inflation;
        
        // 3. Add +/- 10% variance for a more natural feel
        float variance = Random.Range(0.9f, 1.1f);
        int finalReward = Mathf.RoundToInt(rawReward * variance);

        Debug.Log($"Enemy Defeated | Tier: {enemyTier}, Lvl: {enemyLevel} | World Diff: {inflation:F2} | Final: ${finalReward}");
        
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(finalReward);
        }
    }
}