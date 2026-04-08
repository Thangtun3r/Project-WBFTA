using UnityEngine;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class EnemySensor : MonoBehaviour, ITargetSensor
    {
        private EnemyConfig config;
        
        private Transform player;

        private void Awake()
        {
            player = GameObject.FindWithTag("Player")?.transform;
            config = GetComponentInParent<BaseEnemy>()?.Config;
        }

        public bool HasTarget => player != null;
        public Vector3 TargetPosition => player != null ? player.position : Vector3.zero;

        private float GetDistanceToTarget()
        {
            if (!HasTarget) return float.MaxValue;
            return Vector2.Distance(transform.position, player.position);
        }

        public bool IsTargetInDetectionRange()
        {
            if (config == null) return false;
            return GetDistanceToTarget() <= config.detectionRange;
        }

        public bool IsTargetInAttackRange()
        {
            if (config == null) return false;
            return GetDistanceToTarget() <= config.attackRange;
        }

        public bool IsTargetOutOfAttackRange()
        {
            if (config == null) return true;
            return GetDistanceToTarget() > config.attackRange + config.attackExitBuffer;
        }

        public bool IsTargetTooClose()
        {
            if (config == null) return false;
            return GetDistanceToTarget() <= config.stopDistance;
        }

        private void OnDrawGizmos()
        {
            if (config == null) return;

            // Draw detection range
            Gizmos.color = Color.yellow;
            DrawCircleGizmo(transform.position, config.detectionRange, 32);

            // Draw attack range
            Gizmos.color = Color.red;
            DrawCircleGizmo(transform.position, config.attackRange, 32);

            // Draw stop distance
            Gizmos.color = Color.green;
            DrawCircleGizmo(transform.position, config.stopDistance, 16);

            // Draw patrol reach (360-degree patrol with 5 unit reach)
            Gizmos.color = Color.cyan;
            DrawCircleGizmo(transform.position, 5f, 32);

            // Draw enemy avoidance radius
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            DrawCircleGizmo(transform.position, 2f, 16);
        }

        private void DrawCircleGizmo(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}