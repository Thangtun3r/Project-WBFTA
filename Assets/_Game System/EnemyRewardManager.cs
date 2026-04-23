using UnityEngine;

public class EnemyRewardManager : MonoBehaviour
{
    public static EnemyRewardManager Instance { get; private set; }
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


    public void HandleReward(int enemyTier, int enemyLevel)
    {
        float rawReward = baseReward * enemyTier * enemyLevel;
        
        // Add +/- 10% randomness
        float variance = Random.Range(0.9f, 1.1f);
        int finalReward = Mathf.RoundToInt(rawReward * variance);

        Debug.Log($"Enemy Defeated | Tier: {enemyTier}, Level: {enemyLevel} | Base: {rawReward:F1}, Final: {finalReward}");
        EconomyManager.Instance.AddMoney(finalReward);
    }
    
}