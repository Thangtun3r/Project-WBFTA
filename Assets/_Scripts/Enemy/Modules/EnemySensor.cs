using UnityEngine;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class EnemySensor : MonoBehaviour, ITargetSensor
    {
        private EnemyConfig config;
        private Transform _player;
        private Camera _mainCamera;

        [Header("Viewport Optimization")]
        [Tooltip("Extra margin outside the screen (0.1 = 10%) so enemies don't 'deactivate' the millisecond they touch the edge.")]
        [SerializeField] private float viewportBuffer = 0.1f; 

        private void Awake()
        {
            config = GetComponentInParent<BaseEnemy>()?.Config;
            _mainCamera = Camera.main;
        }

        public bool HasTarget 
        {
            get
            {
                if (_player == null)
                {
                    // Finds the player dynamically (fixes issues with pooled/spawned players)
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null) _player = playerObj.transform;
                }
                return _player != null;
            }
        }
        
        public Vector3 TargetPosition => HasTarget ? _player.position : Vector3.zero;

        /// <summary>
        /// Checks if this enemy is currently visible to the player's camera.
        /// </summary>
        public bool IsInViewport()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return false;

            Vector3 viewPos = _mainCamera.WorldToViewportPoint(transform.position);

            // Viewport coordinates are 0 to 1. We add a buffer so they don't 'pop' out.
            bool inX = viewPos.x >= -viewportBuffer && viewPos.x <= 1f + viewportBuffer;
            bool inY = viewPos.y >= -viewportBuffer && viewPos.y <= 1f + viewportBuffer;
            
            // viewPos.z > 0 ensures the object isn't BEHIND the camera
            return inX && inY && viewPos.z > 0;
        }

        private float GetDistanceToTarget()
        {
            if (!HasTarget) return float.MaxValue;
            return Vector2.Distance(transform.position, _player.position);
        }

        // --- ITargetSensor Implementation ---

        public bool IsTargetInDetectionRange()
        {
            // NEW LOGIC: Enemy detects player ONLY if the enemy is on screen
            return HasTarget && IsInViewport();
        }

        public bool IsTargetInAttackRange()
        {
            if (config == null) return false;
            return GetDistanceToTarget() <= config.attackRange;
        }

        public bool IsTargetOutOfAttackRange()
        {
            if (config == null) return true;
            // Includes the exit buffer to prevent 'stuttering' at the edge of the range
            return GetDistanceToTarget() > config.attackRange + config.attackExitBuffer;
        }

        public bool IsTargetTooClose()
        {
            if (config == null) return false;
            return GetDistanceToTarget() <= config.stopDistance;
        }

        // --- Visual Debugging ---

        private void OnDrawGizmos()
        {
            if (config == null) return;

            // Attack Range (Red)
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            DrawCircleGizmo(transform.position, config.attackRange, 32);

            // Stop Distance (Green)
            Gizmos.color = Color.green;
            DrawCircleGizmo(transform.position, config.stopDistance, 16);
            
            // Logic Note: We don't draw a 'Detection Range' anymore because it's the screen!
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