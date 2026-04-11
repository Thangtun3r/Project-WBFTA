using UnityEngine;

namespace _Scripts.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 3f;
        public float acceleration = 12f;
        public float deceleration = 15f;
        public float stopDistance = 3f;
        
        [Header("Detection & Combat")]
        public float detectionRange = 7f;
        public float attackRange = 5f;
        public float attackExitBuffer = 1f;
        public float damage = 10;
        
        [Header("Progression")]
        public int tier = 1;
        public int level = 1;
        public float maxHealth = 100f;
        public float healthIncreasePerLevel = 10f;
        public float damageIncreasePerLevel = 2;
    }
}
