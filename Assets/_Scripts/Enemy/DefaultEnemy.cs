using UnityEngine;
using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public class DefaultEnemy : BaseEnemy
    {
        protected override void Awake()
        {
            base.Awake();
            
            var movement = GetComponent<IMovement>();
            var sensor = GetComponent<ITargetSensor>();
            var attack = GetComponent<IEnemyAttack>();
            var patrol = GetComponent<IPatrolModule>();

            if (_fsm != null)
            {
                _fsm.Initialize(movement, sensor, attack, patrol);
            }
        }
    }
}