using _Scripts.Enemy.Modules;
using UnityEngine;

namespace _Scripts.Enemy
{
    public class EnemyFSM : StateMachine<EnemyFSM.EnemyState>
    {
        public enum EnemyState { Patrol, Chase, Attack, Dead }

        private EnemyStatusController _status;

        private void Awake()
        {
            _status = EnemyStatusController.FindFor(this);
        }

        protected override void Update()
        {
            if (_status == null)
                _status = EnemyStatusController.FindFor(this);

            if (_status != null && !_status.CanThink)
                return;

            base.Update();
        }

        public void Initialize(IMovement movement, ITargetSensor sensor, IEnemyAttack attackModule, IPatrolModule patrol)
        {
            States.Clear();
            States.Add(EnemyState.Patrol, new PatrolState(sensor, patrol, QueueNextState));
            States.Add(EnemyState.Chase, new ChaseState(sensor, movement, QueueNextState));
            States.Add(EnemyState.Attack, new AttackState(sensor, movement, attackModule, QueueNextState));
            States.Add(EnemyState.Dead, new DeadState(movement, attackModule));

            CurrentState = States[EnemyState.Patrol];
        }

        public void ForceState(EnemyState state, bool reenterIfCurrent = false)
        {
            if (reenterIfCurrent && CurrentState != null && CurrentState.StateKey.Equals(state))
            {
                CurrentState.ExitState();
                CurrentState.EnterState();
                return;
            }

            TransitionToState(state);
        }

        public void ReenterCurrentState()
        {
            if (CurrentState == null)
                return;

            CurrentState.ExitState();
            CurrentState.EnterState();
        }
    }
}
