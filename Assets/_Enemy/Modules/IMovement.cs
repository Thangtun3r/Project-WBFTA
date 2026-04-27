using UnityEngine;

namespace _Scripts.Enemy.Modules
{
    public interface IMovement
    {
        void MoveTowards(Vector2 targetPosition);
        void MoveInDirection(Vector2 direction);
        void Stop();
        Vector2 CurrentVelocity { get; }
    }
}