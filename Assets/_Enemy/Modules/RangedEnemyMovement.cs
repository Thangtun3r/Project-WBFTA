using UnityEngine;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class RangedEnemyMovement : MonoBehaviour, IMovement
    {
        private EnemyConfig config;
        
        private Vector2 _currentVelocity;
        public Vector2 CurrentVelocity => _currentVelocity;

        [Header("Collision Avoidance")]
        private bool useCollisionAvoidance = true;
        private float separationRadius = 6f;  // How far to detect other enemies
        private float separationStrength = 12f;  // How strong the repulsion force is
        [SerializeField] private LayerMask enemyLayer;  // Layer to detect other enemies

        private void Awake()
        {
            config = GetComponentInParent<BaseEnemy>()?.Config;
        }

        private void OnEnable()
        {
            _currentVelocity = Vector2.zero;
        }

        public void MoveTowards(Vector2 targetPosition)
        {
            if (config == null) return;
            
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            MoveInDirection(direction);
        }

        public void MoveInDirection(Vector2 direction)
        {
            if (config == null) return;
            
            Vector2 targetVelocity = direction.normalized * config.moveSpeed;
            
            // Apply collision avoidance
            if (useCollisionAvoidance)
            {
                Vector2 separationForce = GetSeparationForce();
                targetVelocity += separationForce;
            }
            
            _currentVelocity = Vector2.MoveTowards(
                _currentVelocity,
                targetVelocity,
                config.acceleration * Time.deltaTime
            );

            ApplyMovement();
        }

        public void Stop()
        {
            if (config == null) return;
            
            _currentVelocity = Vector2.MoveTowards(_currentVelocity, Vector2.zero, config.deceleration * Time.deltaTime);
            ApplyMovement();
        }

        private void ApplyMovement()
        {
            transform.position += (Vector3)_currentVelocity * Time.deltaTime;
        }

        private Vector2 GetSeparationForce()
        {
            Vector2 separationForce = Vector2.zero;
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);
            
            int enemyCount = 0;
            foreach (Collider2D collider in nearbyColliders)
            {
                // Skip self
                if (collider.gameObject == gameObject) continue;
                
                // Calculate direction away from this enemy
                Vector2 directionAway = ((Vector2)transform.position - (Vector2)collider.transform.position).normalized;
                
                // Weight by distance (closer = stronger repulsion)
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                float weight = 1f - Mathf.Clamp01(distance / separationRadius);
                
                separationForce += directionAway * weight;
                enemyCount++;
            }
            
            // Average and scale
            if (enemyCount > 0)
            {
                separationForce = separationForce.normalized * separationStrength;
            }
            
            return separationForce;
        }
    }
}
