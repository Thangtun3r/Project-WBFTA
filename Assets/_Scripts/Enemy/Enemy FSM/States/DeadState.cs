using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public class DeadState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly IMovement movement;
        private readonly IEnemyAttack attackModule;

        public DeadState(IMovement movement, IEnemyAttack attackModule) : base(EnemyFSM.EnemyState.Dead)
        {
            this.movement = movement;
            this.attackModule = attackModule;
        }

        public override void EnterState()
        {
            // Now DeadState is purely logic-focused. 
            // It just ensures the enemy stops attacking and moving.
            attackModule?.SetAttackActive(false);
            movement?.Stop();
        }

        public override void UpdateState()
        {
        }

        public override void ExitState()
        {
        }
    }
}
