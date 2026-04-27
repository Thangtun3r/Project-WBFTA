using UnityEngine;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class RangedEnemyMovement : MonoBehaviour, IMovement
    {
        private EnemyConfig config;
        
        private Vector2 _currentVelocity;
        public Vector2 CurrentVelocity => _currentVelocity;

        private void Awake()
        {
            config = GetComponentInParent<BaseEnemy>()?.Config;
        }

        private void OnEnable()
        {
            _currentVelocity = Vector2.zero;
        }

        public void MoveTowards(Vector2 targetPosition)
        {
            if (config == null) return;
            
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            MoveInDirection(direction);
        }

        public void MoveInDirection(Vector2 direction)
        {
            if (config == null) return;
            
            Vector2 targetVelocity = direction.normalized * config.moveSpeed;
            
            _currentVelocity = Vector2.MoveTowards(
                _currentVelocity,
                targetVelocity,
                config.acceleration * Time.deltaTime
            );

            ApplyMovement();
        }

        public void Stop()
        {
            if (config == null) return;
            
            _currentVelocity = Vector2.MoveTowards(_currentVelocity, Vector2.zero, config.deceleration * Time.deltaTime);
            ApplyMovement();
        }

        private void ApplyMovement()
        {
            transform.position += (Vector3)_currentVelocity * Time.deltaTime;
        }
    }
}
