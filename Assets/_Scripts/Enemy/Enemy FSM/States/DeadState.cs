namespace _Scripts.Enemy
{
    public class DeadState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly EnemyFSM enemyFSM;

        public DeadState(EnemyFSM fsm) : base(EnemyFSM.EnemyState.Dead)
        {
            enemyFSM = fsm;
        }

        public override void EnterState()
        {
            enemyFSM.AttackModule?.SetAttackActive(false);

            if (enemyFSM.EnemyCollider != null)
            {
                enemyFSM.EnemyCollider.enabled = false;
            }

            if (enemyFSM.Visuals != null)
            {
                enemyFSM.Visuals.PlayDeathEffects();
                enemyFSM.Visuals.HideVisual();
            }

            enemyFSM.ScheduleReturnToPool(enemyFSM.DeathReturnDelay);
        }

        public override void UpdateState()
        {
        }

        public override void ExitState()
        {
        }
    }
}
