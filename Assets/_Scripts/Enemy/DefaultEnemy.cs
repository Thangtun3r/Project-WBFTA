using System.Collections;
using UnityEngine;

namespace _Scripts.Enemy
{
    public class DefaultEnemy : BaseEnemy
    {
        [Header("Visuals & Stats")]
        [SerializeField] private SpriteRenderer enemyVisual;
        [SerializeField] private float _health;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float stoppingDistance = 1.5f;

        private Transform _playerTransform;
        private Coroutine _flashCoroutine;

        private void Start()
        {
            // Find the player once at the start
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
            // Calculate distance to player
            float distance = Vector2.Distance(transform.position, _playerTransform.position);

            // Only move if the enemy is further away than the stopping distance
            if (distance > stoppingDistance)
            {
                // Move toward the player position
                transform.position = Vector2.MoveTowards(
                    transform.position, 
                    _playerTransform.position, 
                    moveSpeed * Time.deltaTime
                );
                
                // Optional: Flip the sprite to face the player
                FlipVisual();
            }
        }

        private void FlipVisual()
        {
            if (_playerTransform.position.x < transform.position.x)
                enemyVisual.flipX = true; // Facing left
            else
                enemyVisual.flipX = false; // Facing right
        }

        public override void TakeDamage(float damage)
        {
            if (enemyVisual != null)
            {
                Color currentDefault = Color.white; // Usually better to define a base color or use the existing one
                if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); }
                _flashCoroutine = StartCoroutine(FlashWhiteRoutine(0.15f, Color.white)); // Assuming white is default
            }
            
            _health -= damage;
            if (_health <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashWhiteRoutine(float duration, Color originalColor)
        {
            enemyVisual.color = Color.red; // Changed to red for damage feedback
            yield return new WaitForSeconds(duration);
            enemyVisual.color = originalColor;
            _flashCoroutine = null;
        }
    }
}