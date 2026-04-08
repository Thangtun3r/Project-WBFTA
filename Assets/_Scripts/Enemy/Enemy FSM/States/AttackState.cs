using UnityEngine;
using System;
using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public class AttackState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly ITargetSensor sensor;
        private readonly IMovement movement;
        private readonly IEnemyAttack attackModule;
        private readonly Action<EnemyFSM.EnemyState> changeState;

        public AttackState(ITargetSensor sensor, IMovement movement, IEnemyAttack attackModule, Action<EnemyFSM.EnemyState> changeState) : base(EnemyFSM.EnemyState.Attack)
        {
            this.sensor = sensor;
            this.movement = movement;
            this.attackModule = attackModule;
            this.changeState = changeState;
        }

        public override void EnterState()
        {
            attackModule?.SetAttackActive(true);
        }

        public override void UpdateState()
        {
            if (attackModule == null || sensor == null) return;

            // Execution: Module handles stopping physics
            movement?.Stop();

            // Central decision point: Has the player successfully escaped the attack?
            if (sensor.IsTargetOutOfAttackRange())
            {
                changeState(EnemyFSM.EnemyState.Chase);
            }
        }

        public override void ExitState()
        {
            attackModule?.SetAttackActive(false);
        }
    }
}
