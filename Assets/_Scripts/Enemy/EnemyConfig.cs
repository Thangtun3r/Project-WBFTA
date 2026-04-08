using UnityEngine;

namespace _Scripts.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public float moveSpeed = 3f;
        public float acceleration = 12f;
        public float deceleration = 15f;
        public float stopDistance = 1.5f;
        public float detectionRange = 5f;
        public float attackRange = 1.2f;
        public float attackExitBuffer = 0.2f;
    }
}
