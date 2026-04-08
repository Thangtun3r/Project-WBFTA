using UnityEngine;

namespace _Scripts.Enemy.Modules
{
    public class EnemyPatrol : MonoBehaviour, IPatrolModule
    {
        private EnemyConfig config;
        [SerializeField] private float patrolDistance = 5f;
        [SerializeField] private float stopDuration = 1.5f;
        [SerializeField] private float enemyAvoidanceRadius = 2f;
        [SerializeField] private LayerMask enemyLayer;

        private IMovement _movement;
        private Vector2 _currentDirection;
        private float _moveTimer = 0f;
        private float _stopTimer = 0f;
        private bool _isMoving = true;

        private void Awake()
        {
            _movement = GetComponent<IMovement>();
            config = GetComponentInParent<BaseEnemy>()?.Config;
            if (enemyLayer == 0) enemyLayer = LayerMask.GetMask("Default");
        }

        public void StartPatrol()
        {
            PickNewDirection();
            _stopTimer = 0f;
            _isMoving = true;
        }

        public void UpdatePatrol()
        {
            if (config == null || _movement == null) return;

            if (_isMoving)
            {
                if (IsEnemyAhead())
                {
                    PickNewDirection();
                }

                _moveTimer -= Time.deltaTime;
                _movement.MoveInDirection(_currentDirection);

                if (_moveTimer <= 0)
                {
                    _isMoving = false;
                    _stopTimer = stopDuration;
                }
            }
            else
            {
                _stopTimer -= Time.deltaTime;
                _movement.Stop();

                if (_stopTimer <= 0)
                {
                    _isMoving = true;
                    PickNewDirection();
                }
            }
        }

        public void StopPatrol()
        {
            _movement?.Stop();
        }

        private void PickNewDirection()
        {
            float randomAngle = Random.Range(0f, 360f);
            _currentDirection = new Vector2(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                Mathf.Sin(randomAngle * Mathf.Deg2Rad)
            );
            
            if (config != null)
                _moveTimer = patrolDistance / config.moveSpeed;
        }

        private bool IsEnemyAhead()
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, _currentDirection, enemyAvoidanceRadius, enemyLayer);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.transform != transform)
                {
                    if (hit.collider.GetComponent<EnemyFSM>() != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}