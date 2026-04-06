using UnityEngine;

namespace _Scripts.Enemy
{
    [RequireComponent(typeof(EnemyVisuals))]
    public class DefaultEnemy : BaseEnemy
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float stoppingDistance = 1.5f;

        private Transform _playerTransform;
        private EnemyVisuals _visuals;
        private Collider2D _enemyCollider;

        protected override void Awake()
        {
            base.Awake();
            _visuals = GetComponent<EnemyVisuals>();
            _enemyCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (_playerTransform != null)
            {
                FollowPlayer();
            }
        }

        private void FollowPlayer()
        {
            float distance = Vector2.Distance(transform.position, _playerTransform.position);

            if (distance > stoppingDistance)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position, 
                    _playerTransform.position, 
                    moveSpeed * Time.deltaTime
                );
                
                FlipVisual();
            }
        }

        private void FlipVisual()
        {
            bool shouldFlip = _playerTransform.position.x < transform.position.x;
            _visuals.Flip(shouldFlip);
        }

        public override void TakeDamage(float damage)
        {
            currentHealth -= damage;
            
            if (currentHealth > 0)
            {
                _visuals.PlayHitEffects();
            }
            else
            {
                PerformDeath();
            }
        }

        private void PerformDeath()
        {
            // 1. Turn off gameplay mechanics so it can't move or be hit again
            moveSpeed = 0f;
            if (_enemyCollider != null) _enemyCollider.enabled = false;

            // 2. Play the burst and hide the body
            _visuals.PlayDeathEffects();
            _visuals.HideVisual();

            // 3. Wait 2 seconds for the particles to finish, THEN clean it up
            Destroy(gameObject, 2f); 
        }
    }
}