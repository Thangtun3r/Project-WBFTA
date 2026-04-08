using _Scripts.Enemy.Modules;
using UnityEngine;

namespace _Scripts.Enemy
{
    public class EnemyFSM : StateMachine<EnemyFSM.EnemyState>
    {
        public enum EnemyState { Patrol, Chase, Attack, Dead }

        public void Initialize(IMovement movement, ITargetSensor sensor, IEnemyAttack attackModule, IPatrolModule patrol)
        {
            States.Add(EnemyState.Patrol, new PatrolState(sensor, patrol, QueueNextState));
            States.Add(EnemyState.Chase, new ChaseState(sensor, movement, QueueNextState));
            States.Add(EnemyState.Attack, new AttackState(sensor, movement, attackModule, QueueNextState));
            States.Add(EnemyState.Dead, new DeadState(movement, attackModule));

            CurrentState = States[EnemyState.Patrol];
        }
    }
}