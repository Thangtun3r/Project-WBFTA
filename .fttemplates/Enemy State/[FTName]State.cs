using UnityEngine;
using System;
using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public class [FTName]State : BaseState<EnemyFSM.EnemyState>
    {
        // Add required module interfaces here (e.g., ITargetSensor, IMovement)
        private readonly Action<EnemyFSM.EnemyState> changeState;

        public [FTName]State(Action<EnemyFSM.EnemyState> changeState) : base(EnemyFSM.EnemyState.[FTName])
        {
            // add more modules to the constructor as needed and assign them to private fields
            this.changeState = changeState;
        }

        public override void EnterState()
        {
            // Called once when entering the state
        }

        public override void UpdateState()
        {
            // Called every frame. 
            // Use modules for execution and trigger changeState() based on conditions.
            
            /* Example:
            if (someCondition)
            {
                changeState(EnemyFSM.EnemyState.AnotherState);
            }
            */
        }

        public override void ExitState()
        {
            // Called once when exiting the state
        }
    }
}
