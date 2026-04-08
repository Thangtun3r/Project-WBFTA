using UnityEngine;
using System;
using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public class ChaseState : BaseState<EnemyFSM.EnemyState>
    {
        private readonly ITargetSensor sensor;
        private readonly IMovement movement;
        private readonly Action<EnemyFSM.EnemyState> changeState;

        public ChaseState(ITargetSensor sensor, IMovement movement, Action<EnemyFSM.EnemyState> changeState) : base(EnemyFSM.EnemyState.Chase)
        {
            this.sensor = sensor;
            this.movement = movement;
            this.changeState = changeState;
        }

        public override void EnterState()
        {
            Debug.Log("ChaseState: Entered");
        }

        public override void UpdateState()
        {
            if (sensor == null || movement == null || !sensor.HasTarget)
            {
                return;
            }

            // Return to Patrol if player goes out of detection range
            if (!sensor.IsTargetInDetectionRange())
            {
                changeState(EnemyFSM.EnemyState.Patrol);
                return;
            }
            
            // Transition to Attack if within attack range
            if (sensor.IsTargetInAttackRange())
            {
                // Removed movement.Stop() here so the enemy maintains momentum
                changeState(EnemyFSM.EnemyState.Attack);
                return;
            }

            // Stop if too close, else move towards target
            if (sensor.IsTargetTooClose() && !(movement is _Scripts.Enemy.Modules.DiveEnemyMovement))
            {
                movement.Stop();
            }
            else
            {
                movement.MoveTowards(sensor.TargetPosition);
            }
        }

        public override void ExitState()
        {
            Debug.Log("ChaseState: Exited");
        }
    }
}
