using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "ScriptableObjects/PlayerStats", order = 1)]
public class PlayerStats: ScriptableObject
{
    [Header("Player Stats")]
    public int health = 100;
    public int maxHealth = 100;
    public int damage = 10;
    public float moveSpeed = 5f;

}
