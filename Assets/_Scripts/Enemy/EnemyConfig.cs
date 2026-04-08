using UnityEngine;

namespace _Scripts.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public float moveSpeed = 3f;
        public float acceleration = 12f;
        public float deceleration = 15f;
        public float stopDistance = 3f;
        public float detectionRange = 7f;
        public float attackRange = 5f;
        public float attackExitBuffer = 1f;
        public int damage = 10;
    }
}
