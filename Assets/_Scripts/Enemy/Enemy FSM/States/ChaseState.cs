using UnityEngine;

namespace _Scripts.Enemy
{
    public class ChaseState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly EnemyFSM enemyFSM;

        public ChaseState(EnemyFSM fsm) : base(EnemyFSM.EnemyState.Chase)
        {
            enemyFSM = fsm;
        }

        public override void EnterState()
        {
            Debug.Log("ChaseState: Entered");
        }

        public override void UpdateState()
        {
            if (enemyFSM.player == null || enemyFSM.Config == null)
            {
                return;
            }

            float dist = Vector2.Distance(enemyFSM.transform.position, enemyFSM.player.position);
            
            // Return to Patrol if player goes out of detection range
            if (dist > enemyFSM.Config.detectionRange)
            {
                Debug.Log($"ChaseState: Player out of range ({dist:F2} > {enemyFSM.Config.detectionRange}). Returning to Patrol.");
                enemyFSM.QueueNextState(EnemyFSM.EnemyState.Patrol);
                return;
            }
            
            // Transition to Attack if within attack range
            if (dist <= enemyFSM.Config.attackRange)
            {
                enemyFSM.CurrentVelocity = Vector2.MoveTowards(enemyFSM.CurrentVelocity, Vector2.zero, enemyFSM.Config.deceleration * Time.deltaTime);
                enemyFSM.QueueNextState(EnemyFSM.EnemyState.Attack);
                return;
            }

            // Stop if too close
            if (dist <= enemyFSM.Config.stopDistance)
            {
                enemyFSM.CurrentVelocity = Vector2.MoveTowards(enemyFSM.CurrentVelocity, Vector2.zero, enemyFSM.Config.deceleration * Time.deltaTime);
            }
            else
            {
                // Chase the player
                Vector2 direction = (enemyFSM.player.position - enemyFSM.transform.position).normalized;
                Vector2 targetVelocity = direction * enemyFSM.Config.moveSpeed;
                enemyFSM.CurrentVelocity = Vector2.MoveTowards(
                    enemyFSM.CurrentVelocity,
                    targetVelocity,
                    enemyFSM.Config.acceleration * Time.deltaTime
                );
            }

            // Apply movement
            enemyFSM.transform.position += (Vector3)enemyFSM.CurrentVelocity * Time.deltaTime;
        }

        public override void ExitState()
        {
            Debug.Log("ChaseState: Exited");
        }
    }
}
