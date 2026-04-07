using UnityEngine;

namespace _Scripts.Enemy
{
    public class PatrolState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly EnemyFSM enemyFSM;
        private Vector2 currentDirection;
        private float patrolDistance = 5f;
        private float stopDuration = 1.5f;
        private float moveTimer = 0f;
        private float stopTimer = 0f;
        private bool isMoving = true;
        private float enemyAvoidanceRadius = 2f;
        private LayerMask enemyLayer;

        public PatrolState(EnemyFSM fsm) : base(EnemyFSM.EnemyState.Patrol)
        {
            enemyFSM = fsm;
        }

        public override void EnterState()
        {
            Debug.Log("PatrolState: Entered - Starting 360° patrol with collision avoidance");
            enemyLayer = LayerMask.GetMask("Default"); // Adjust if enemies are on a different layer
            PickNewDirection();
            // Removed moveTimer = 0f so it actually moves initially
            stopTimer = 0f;
            isMoving = true;
        }

        private void PickNewDirection()
        {
            // Random direction in any 360-degree angle
            float randomAngle = Random.Range(0f, 360f);
            currentDirection = new Vector2(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                Mathf.Sin(randomAngle * Mathf.Deg2Rad)
            );
            moveTimer = patrolDistance / enemyFSM.Config.moveSpeed;
            Debug.Log($"PatrolState: New direction = {randomAngle:F0}°, will move for {moveTimer:F2}s");
        }

        private bool IsEnemyAhead()
        {
            // Check for other enemies ahead in the current direction
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                enemyFSM.transform.position,
                currentDirection,
                enemyAvoidanceRadius
            );

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != enemyFSM.gameObject)
                {
                    // Check if it's an enemy (has EnemyFSM component)
                    if (hit.collider.GetComponent<EnemyFSM>() != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void UpdateState()
        {
            if (enemyFSM.player == null || enemyFSM.Config == null)
            {
                return;
            }

            // Check player detection
            float distToPlayer = Vector2.Distance(enemyFSM.transform.position, enemyFSM.player.position);
            if (distToPlayer <= enemyFSM.Config.detectionRange)
            {
                Debug.Log($"PatrolState: Player detected! Transitioning to Chase");
                isMoving = false;
                enemyFSM.QueueNextState(EnemyFSM.EnemyState.Chase);
                return;
            }

            // Handle movement phase
            if (isMoving)
            {
                // Check if enemy is ahead and avoid
                if (IsEnemyAhead())
                {
                    Debug.Log("PatrolState: Enemy detected ahead, picking new direction");
                    PickNewDirection();
                }

                moveTimer -= Time.deltaTime;
                
                // Accelerate in current direction
                Vector2 targetVelocity = currentDirection * enemyFSM.Config.moveSpeed;
                enemyFSM.CurrentVelocity = Vector2.MoveTowards(
                    enemyFSM.CurrentVelocity,
                    targetVelocity,
                    enemyFSM.Config.acceleration * Time.deltaTime
                );

                // Time to stop
                if (moveTimer <= 0)
                {
                    isMoving = false;
                    stopTimer = stopDuration;
                    Debug.Log($"PatrolState: Stopping, will wait {stopDuration}s");
                }
            }
            // Handle stop phase
            else
            {
                stopTimer -= Time.deltaTime;
                
                // Decelerate to stop
                enemyFSM.CurrentVelocity = Vector2.MoveTowards(
                    enemyFSM.CurrentVelocity,
                    Vector2.zero,
                    enemyFSM.Config.deceleration * Time.deltaTime
                );
                
                if (stopTimer <= 0)
                {
                    isMoving = true;
                    PickNewDirection();
                }
            }

            // Apply movement
            enemyFSM.transform.position += (Vector3)enemyFSM.CurrentVelocity * Time.deltaTime;
        }

        public override void ExitState()
        {
            Debug.Log("PatrolState: Exited");
        }
    }
}