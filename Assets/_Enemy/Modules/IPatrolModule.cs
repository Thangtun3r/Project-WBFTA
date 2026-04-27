using UnityEngine;

namespace _Scripts.Enemy.Modules
{
    public interface IPatrolModule
    {
        void StartPatrol();
        void UpdatePatrol();
        void StopPatrol();
    }
}