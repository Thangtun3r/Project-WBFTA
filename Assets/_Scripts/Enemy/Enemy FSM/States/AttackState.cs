using UnityEngine;

namespace _Scripts.Enemy
{
    public class AttackState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly EnemyFSM enemyFSM;

        public AttackState(EnemyFSM fsm) : base(EnemyFSM.EnemyState.Attack)
        {
            enemyFSM = fsm;
        }

        public override void EnterState()
        {
            enemyFSM.AttackModule?.SetAttackActive(true);
        }

        public override void UpdateState()
        {
            // Decelerate to stop while attacking
            if (enemyFSM.CurrentVelocity.sqrMagnitude > 0.01f)
            {
                enemyFSM.CurrentVelocity = Vector2.MoveTowards(
                    enemyFSM.CurrentVelocity,
                    Vector2.zero,
                    enemyFSM.Config.deceleration * Time.deltaTime
                );
                enemyFSM.transform.position += (Vector3)enemyFSM.CurrentVelocity * Time.deltaTime;
            }

            if (enemyFSM.player == null || enemyFSM.Config == null)
            {
                return;
            }

            float dist = Vector2.Distance(enemyFSM.transform.position, enemyFSM.player.position);
            if (dist > enemyFSM.Config.attackRange + enemyFSM.AttackExitBuffer)
            {
                enemyFSM.QueueNextState(EnemyFSM.EnemyState.Chase);
            }
        }

        public override void ExitState()
        {
            enemyFSM.AttackModule?.SetAttackActive(false);
        }
    }
}
