using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Enemy
{

    public class DefaultEnemy : BaseEnemy
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float stoppingDistance = 1.5f;

        private Transform _playerTransform;
        private EnemyVisuals _visuals;
        private Collider2D _enemyCollider;
        private HashSet<Collider2D> _playerBlockingColliders = new HashSet<Collider2D>();
        private Vector2 _moveDirection;

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
            if (_playerTransform == null)
                return;

            UpdateMoveDirection();

            if (_playerBlockingColliders.Count == 0)
            {
                FollowPlayer();
            }
        }

        private void UpdateMoveDirection()
        {
            Vector2 toPlayer = _playerTransform.position - transform.position;
            float distance = toPlayer.magnitude;
            _moveDirection = distance > stoppingDistance ? toPlayer.normalized : Vector2.zero;
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
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsBlockingPlayerCollision(collision))
                return;

            _playerBlockingColliders.Add(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;
            if (!collision.collider.CompareTag("Player")) return;

            if (IsCollisionBlockingMovement(collision))
            {
                _playerBlockingColliders.Add(collision.collider);
            }
            else
            {
                _playerBlockingColliders.Remove(collision.collider);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;
            if (!collision.collider.CompareTag("Player")) return;

            _playerBlockingColliders.Remove(collision.collider);
        }

        private bool IsBlockingPlayerCollision(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return false;
            if (!collision.collider.CompareTag("Player")) return false;
            return IsCollisionBlockingMovement(collision);
        }

        private bool IsCollisionBlockingMovement(Collision2D collision)
        {
            if (_moveDirection == Vector2.zero)
                return false;

            if (collision.contacts.Length > 0)
            {
                foreach (var contact in collision.contacts)
                {
                    if (Vector2.Dot(_moveDirection, -contact.normal) > 0.5f)
                        return true;
                }
            }

            if (collision.relativeVelocity.sqrMagnitude > 0.001f)
            {
                return Vector2.Dot(collision.relativeVelocity.normalized, _moveDirection) > 0.5f;
            }

            return false;
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

            // 3. Wait 2 seconds for the particles to finish, THEN return it to the pool
            StartCoroutine(ReturnToPoolAfterDelay(2f));
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Spawner spawner = FindObjectOfType<Spawner>();
            if (spawner != null)
            {
                spawner.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}