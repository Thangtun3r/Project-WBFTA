using UnityEngine;

namespace _Scripts.Enemy.Modules
{
    public interface ITargetSensor
    {
        bool HasTarget { get; }
        Vector3 TargetPosition { get; }
        
        bool IsTargetInDetectionRange();
        bool IsTargetInAttackRange();
        bool IsTargetOutOfAttackRange();
        bool IsTargetTooClose();
    }
}