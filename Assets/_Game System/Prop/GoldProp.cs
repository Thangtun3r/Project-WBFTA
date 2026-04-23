using UnityEngine;


public class GoldProp : PropBase
{
    [Header("Gold Reward")]
    [SerializeField] private int baseGoldReward = 50;
    [SerializeField] private float goldPerLevel = 10f;

    protected override void OnDeathBehavior()
    {
        RewardPlayer();
    }

    private void RewardPlayer()
    {
        int goldReward = Mathf.FloorToInt(baseGoldReward + (currentLevel - 1) * goldPerLevel);
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(goldReward);
            Debug.Log($"GoldProp died at level {currentLevel}: Rewarding {goldReward} gold!");
        }
    }
}
