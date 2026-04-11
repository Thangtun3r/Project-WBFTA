using UnityEngine;
using DG.Tweening;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class DiveEnemyMovement : MonoBehaviour, IMovement
    {
        private EnemyConfig config;
        
        private Vector2 _currentVelocity;
        public Vector2 CurrentVelocity => _currentVelocity;

        [Header("Dive Settings")]
        [Tooltip("How long the enemy aims in place before dashing")]
        [SerializeField] private float lockTime = 1f;
        
        [Tooltip("How long the enemy dashes forward (this plus speed dictates distance)")]
        [SerializeField] private float diveTime = 0.6f;
        
        [Tooltip("Speed multiplier applied to normal moveSpeed during the dash")]
        [SerializeField] private float diveSpeedMultiplier = 3.5f;

        [Header("Visuals")]
        [Tooltip("The object that visibly points in the direction of the dive")]
        [SerializeField] private Transform indicatorRoot;
        [Tooltip("The actual sprite root that will shake to anticipate the dive")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Euler angle limits for the shake (left/right)")]
        [SerializeField] private Vector3 shakeAngle = new Vector3(0, 0, 15f);
        [Tooltip("How fast the shake ticks back and forth")]
        [SerializeField] private float shakeSpeed = 0.05f;
        [Tooltip("What kind of easing curve the shake should use")]
        [SerializeField] private Ease shakeEase = Ease.InOutSine;
        [Tooltip("What color the sprite flashes when winding up")]
        [SerializeField] private Color flashColor = Color.white;
        [Tooltip("How fast the color flashes")]
        [SerializeField] private float flashSpeed = 0.1f;

        private enum DiveState { Locking, Diving, Returning }
        private DiveState _currentState = DiveState.Locking;
        private float _stateTimer = 0f;
        private Vector2 _lockedDirection;
        private Camera _mainCamera;
        private SpriteRenderer _visualSpriteRenderer;
        private Color _originalColor;

        // Tracks whether we've triggered the shake for the current lock phase
        private bool _isShaking = false;

        private void Awake()
        {
            config = GetComponentInParent<BaseEnemy>()?.Config;
            _mainCamera = Camera.main;
            
            if (visualRoot != null)
            {
                _visualSpriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
                if (_visualSpriteRenderer == null)
                    _visualSpriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
                    
                if (_visualSpriteRenderer != null)
                    _originalColor = _visualSpriteRenderer.color;
            }
        }

        private void OnEnable()
        {
            // Reset state for Object Pooling
            _currentState = DiveState.Locking;
            _stateTimer = 0f;
            _isShaking = false;
            if (_visualSpriteRenderer != null)
            {
                _visualSpriteRenderer.DOKill();
                _visualSpriteRenderer.color = _originalColor;
            }
            if (visualRoot != null)
            {
                visualRoot.DOKill();
                visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void OnDisable()
        {
            if (visualRoot != null)
            {
                visualRoot.DOKill();
                visualRoot.localRotation = Quaternion.identity;
            }
            if (_visualSpriteRenderer != null)
            {
                _visualSpriteRenderer.DOKill();
                _visualSpriteRenderer.color = _originalColor;
            }
        }

        private void ChangeState(DiveState newState)
        {
            // Clean up exiting state
            if (_currentState == DiveState.Locking && visualRoot != null)
            {
                visualRoot.DOKill();
                visualRoot.localRotation = Quaternion.identity;
                _isShaking = false;
            }

            // Always make sure flashing stops if we are entering returning or diving
            if (newState != DiveState.Locking)
            {
                if (_visualSpriteRenderer != null)
                {
                    _visualSpriteRenderer.DOKill();
                    _visualSpriteRenderer.color = _originalColor;
                }
            }

            _currentState = newState;
            _stateTimer = 0f;

            // Trigger entering state
            if (_currentState == DiveState.Locking && visualRoot != null)
            {
                // Consistent PingPong rotation: start fully minus, tweet to fully plus and repeat back and forth
                visualRoot.localRotation = Quaternion.Euler(-shakeAngle);
                visualRoot.DOLocalRotate(shakeAngle, shakeSpeed)
                    .SetEase(shakeEase)
                    .SetLoops(-1, LoopType.Yoyo);
                    
                if (_visualSpriteRenderer != null)
                {
                    _visualSpriteRenderer.color = _originalColor;
                    _visualSpriteRenderer.DOColor(flashColor, flashSpeed)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                }

                _isShaking = true;
            }
        }

        public void MoveTowards(Vector2 targetPosition)
        {
            if (config == null) return;
            
            // Constantly update aim while in locking or returning state
            if (_currentState == DiveState.Locking || _currentState == DiveState.Returning)
            {
                _lockedDirection = (targetPosition - (Vector2)transform.position).normalized;
            }
            
            ProcessDiveState();
        }

        public void MoveInDirection(Vector2 direction)
        {
            if (config == null) return;
            
            // Constantly update aim while in locking or returning state
            if (_currentState == DiveState.Locking || _currentState == DiveState.Returning)
            {
                _lockedDirection = direction.normalized;
            }

            ProcessDiveState();
        }

        private void ProcessDiveState()
        {
            _stateTimer += Time.deltaTime;

            // Check if enemy is out of bounds
            bool isOffScreen = false;
            if (_mainCamera != null)
            {
                Vector3 vp = _mainCamera.WorldToViewportPoint(transform.position);
                // 5% margin to consider it safely "inside" the screen
                if (vp.x <= 0.05f || vp.x >= 0.95f || vp.y <= 0.05f || vp.y >= 0.95f)
                {
                    isOffScreen = true;
                }
            }

            // Immediately switch to returning if we are locking but outside the screen
            if (isOffScreen && _currentState == DiveState.Locking)
            {
                ChangeState(DiveState.Returning);
            }
            // Once safely back inside the viewport, switch back to locking
            else if (!isOffScreen && _currentState == DiveState.Returning)
            {
                ChangeState(DiveState.Locking); 
            }

            // Handle States
            if (_currentState == DiveState.Returning)
            {
                // Just walk normally towards the target to re-enter the arena
                Vector2 targetVelocity = _lockedDirection * config.moveSpeed;
                _currentVelocity = Vector2.MoveTowards(
                    _currentVelocity, 
                    targetVelocity, 
                    config.acceleration * Time.deltaTime
                );
                
                // Point indicator towards target safely
                if (indicatorRoot != null && _lockedDirection != Vector2.zero)
                {
                    float angle = Mathf.Atan2(_lockedDirection.y, _lockedDirection.x) * Mathf.Rad2Deg;
                    Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90f);
                    indicatorRoot.rotation = Quaternion.Lerp(indicatorRoot.rotation, targetRotation, 15f * Time.deltaTime);
                }
            }
            else if (_currentState == DiveState.Locking)
            {
                // Ensure shaking starts if this is the very first frame or just entered silently
                if (!_isShaking && visualRoot != null)
                {
                    visualRoot.localRotation = Quaternion.Euler(-shakeAngle);
                    visualRoot.DOLocalRotate(shakeAngle, shakeSpeed)
                        .SetEase(shakeEase)
                        .SetLoops(-1, LoopType.Yoyo);
                        
                    if (_visualSpriteRenderer != null)
                    {
                        _visualSpriteRenderer.color = _originalColor;
                        _visualSpriteRenderer.DOColor(flashColor, flashSpeed)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    }

                    _isShaking = true;
                }

                // Smoothly rotate indicator toward locked direction
                if (indicatorRoot != null && _lockedDirection != Vector2.zero)
                {
                    float angle = Mathf.Atan2(_lockedDirection.y, _lockedDirection.x) * Mathf.Rad2Deg;
                    Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90f); // -90 assumes default sprite points UP
                    indicatorRoot.rotation = Quaternion.Lerp(indicatorRoot.rotation, targetRotation, 15f * Time.deltaTime);
                }

                // Brake and stay still while winding up the attack
                _currentVelocity = Vector2.MoveTowards(
                    _currentVelocity, 
                    Vector2.zero, 
                    config.deceleration * Time.deltaTime
                );

                if (_stateTimer >= lockTime)
                {
                    ChangeState(DiveState.Diving);
                }
            }
            else if (_currentState == DiveState.Diving)
            {
                // Shoot forward like a rocket using the locked direction! (Ignores targets current position so it overshoots)
                Vector2 targetVelocity = _lockedDirection * (config.moveSpeed * diveSpeedMultiplier);
                
                // Accelerate extremely fast to simulate a burst of speed
                _currentVelocity = Vector2.MoveTowards(
                    _currentVelocity, 
                    targetVelocity, 
                    (config.acceleration * diveSpeedMultiplier) * Time.deltaTime
                );

                if (_stateTimer >= diveTime)
                {
                    ChangeState(DiveState.Locking);
                }
            }

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
