using UnityEngine;

namespace _Scripts
{
    [CreateAssetMenu(menuName = "Player/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Combat Stats")]
        [SerializeField] public float damage = 25f;
        
        [Header("Health Stats")]
        [SerializeField] public float maxHealth = 100f;
    }
}
