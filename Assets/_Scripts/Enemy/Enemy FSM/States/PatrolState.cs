using UnityEngine;
using System;
using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public class PatrolState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly ITargetSensor sensor;
        private readonly IPatrolModule patrol;
        private readonly Action<EnemyFSM.EnemyState> changeState;

        public PatrolState(ITargetSensor sensor, IPatrolModule patrol, Action<EnemyFSM.EnemyState> changeState) : base(EnemyFSM.EnemyState.Patrol)
        {
            this.sensor = sensor;
            this.patrol = patrol;
            this.changeState = changeState;
        }

        public override void EnterState()
        {
            patrol?.StartPatrol();
        }

        public override void UpdateState()
        {
            if (sensor == null || patrol == null)
            {
                return;
            }

            // Central decision point: The state just decides the flow
            if (sensor.IsTargetInDetectionRange())
            {
                changeState(EnemyFSM.EnemyState.Chase);
                return;
            }

            // Execution: Handled by the module
            patrol.UpdatePatrol();
        }

        public override void ExitState()
        {
            patrol?.StopPatrol();
        }
    }
}