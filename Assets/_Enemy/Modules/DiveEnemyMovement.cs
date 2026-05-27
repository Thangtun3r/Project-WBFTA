using UnityEngine;
using DG.Tweening;
using _Scripts.Enemy;

namespace _Scripts.Enemy.Modules
{
    public class DiveEnemyMovement : MonoBehaviour, IMovement
    {
        private EnemyConfig config;
        private EnemyStatusController _status;
        private Vector2 _currentVelocity;
        public Vector2 CurrentVelocity => _currentVelocity;

        [Header("Dive Settings")]
        [SerializeField] private float lockTime = 1f;
        [SerializeField] private float graceCommitmentTime = 0.3f;
        [SerializeField] private float diveSpeedMultiplier = 4f;
        private float overshootDistance = 3f; // New: How far past the player to lunge
        [SerializeField] private float stallDuration = 2f;
        [SerializeField] private float arrivalThreshold = 0.2f;
        [SerializeField] private float viewportMargin = 0.1f;

        [Header("Visuals")]
        [SerializeField] private Transform directionRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 shakeAngle = new Vector3(0, 0, 15f);
        [SerializeField] private float shakeSpeed = 0.05f;
        [SerializeField] private Ease shakeEase = Ease.InOutSine;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashSpeed = 0.1f;
        
        [Header("Stuck Effects")]
        [SerializeField] private float crashShakeStrength = 0.3f;
        [SerializeField] private int crashShakeVibrato = 10;
        [SerializeField] private ParticleSystem crashParticleEffect;

        private enum DiveState { Chasing, Locking, Diving, Stuck }
        private DiveState _currentState = DiveState.Chasing;
        
        private float _stateTimer = 0f;
        private Vector2 _diveTarget;
        private bool _isCommitted = false;
        private Camera _mainCamera;
        private SpriteRenderer _visualSpriteRenderer;
        private Color _originalColor;

        private void Awake()
        {
            config = GetComponentInParent<BaseEnemy>()?.Config;
            _status = EnemyStatusController.FindFor(this);
            _mainCamera = Camera.main;
            
            if (visualRoot != null)
            {
                _visualSpriteRenderer = visualRoot.GetComponent<SpriteRenderer>() ?? visualRoot.GetComponentInChildren<SpriteRenderer>();
                if (_visualSpriteRenderer != null) _originalColor = _visualSpriteRenderer.color;
            }
        }

        private void OnEnable()
        {
            ChangeState(DiveState.Chasing);
            _currentVelocity = Vector2.zero;
            _diveTarget = Vector2.zero;
            if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
        }

        private void ChangeState(DiveState newState)
        {
            // Clean up previous state tweens
            if (visualRoot != null) visualRoot.DOKill();
            if (_visualSpriteRenderer != null)
            {
                _visualSpriteRenderer.DOKill();
                _visualSpriteRenderer.color = _originalColor;
            }

            _currentState = newState;
            _stateTimer = 0f;
            _isCommitted = false;

            switch (_currentState)
            {
                case DiveState.Chasing:
                    if (visualRoot != null) visualRoot.localRotation = Quaternion.identity;
                    if (directionRoot != null) directionRoot.gameObject.SetActive(true);
                    break;

                case DiveState.Locking:
                    if (visualRoot != null)
                    {
                        visualRoot.localRotation = Quaternion.Euler(-shakeAngle);
                        visualRoot.DOLocalRotate(shakeAngle, shakeSpeed).SetEase(shakeEase).SetLoops(-1, LoopType.Yoyo);
                    }
                    if (_visualSpriteRenderer != null)
                    {
                        TweenSpriteColor(_visualSpriteRenderer, flashColor, flashSpeed)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    }
                    break;

                case DiveState.Diving:
                    // OVERSHOOT LOGIC: 
                    // Calculate direction toward the target and extend it
                    Vector2 currentPos = transform.position;
                    Vector2 dirToTarget = (_diveTarget - currentPos).normalized;
                    
                    // Update _diveTarget to be past the player
                    _diveTarget = currentPos + (dirToTarget * (Vector2.Distance(currentPos, _diveTarget) + overshootDistance));
                    
                    // Clamp to viewport so they don't dive into the "void"
                    _diveTarget = GetClampedViewportPosition(_diveTarget);
                    break;

                case DiveState.Stuck:
                    _currentVelocity = Vector2.zero;
                    // Ensure we snap to the final target on impact
                    transform.position = _diveTarget;

                    if (directionRoot != null) directionRoot.gameObject.SetActive(false);

                    if (visualRoot != null)
                    {
                        visualRoot.DOShakePosition(stallDuration * 0.5f, crashShakeStrength, crashShakeVibrato);
                    }

                    if (crashParticleEffect != null) crashParticleEffect.Play();
                    break;
            }
        }

        public void MoveTowards(Vector2 targetPosition)
        {
            if (config == null) return;
            if (_status != null && !_status.CanMove)
            {
                Stop();
                return;
            }

            // Only update target if chasing or in the early "grace" period of locking
            if (_currentState == DiveState.Chasing || (_currentState == DiveState.Locking && !_isCommitted))
            {
                _diveTarget = GetClampedViewportPosition(targetPosition);
            }
            ProcessDiveState();
        }

        public void MoveInDirection(Vector2 direction)
        {
            if (config == null) return;
            if (_status != null && !_status.CanMove)
            {
                Stop();
                return;
            }

            if (_currentState == DiveState.Chasing || (_currentState == DiveState.Locking && !_isCommitted))
            {
                Vector2 target = (Vector2)transform.position + (direction.normalized * 5f);
                _diveTarget = GetClampedViewportPosition(target);
            }
            ProcessDiveState();
        }

        private void ProcessDiveState()
        {
            _stateTimer += Time.deltaTime;

            switch (_currentState)
            {
                case DiveState.Chasing:
                    Vector2 chaseDir = (_diveTarget - (Vector2)transform.position).normalized;
                    float speedMultiplier = _status != null ? _status.MoveSpeedMultiplier : 1f;
                    _currentVelocity = Vector2.MoveTowards(_currentVelocity, chaseDir * (config.moveSpeed * speedMultiplier), config.acceleration * Time.deltaTime);
                    UpdateIndicatorRotation();
                    if (IsInsideViewport()) ChangeState(DiveState.Locking);
                    break;

                case DiveState.Locking:
                    _currentVelocity = Vector2.MoveTowards(_currentVelocity, Vector2.zero, config.deceleration * Time.deltaTime);
                    UpdateIndicatorRotation();
                    if (_stateTimer >= graceCommitmentTime) _isCommitted = true;
                    if (_stateTimer >= lockTime) ChangeState(DiveState.Diving);
                    break;

                case DiveState.Diving:
                    float step = (config.moveSpeed * diveSpeedMultiplier) * Time.deltaTime;
                    transform.position = Vector2.MoveTowards(transform.position, _diveTarget, step);
                    
                    // Keep indicator pointing at the overshoot target
                    UpdateIndicatorRotation();

                    if (Vector2.Distance(transform.position, _diveTarget) < arrivalThreshold)
                    {
                        ChangeState(DiveState.Stuck);
                    }
                    break;

                case DiveState.Stuck:
                    if (_stateTimer >= stallDuration * 0.5f)
                    {
                        if (directionRoot != null && !directionRoot.gameObject.activeSelf)
                        {
                            directionRoot.gameObject.SetActive(true);
                        }
                    }

                    if (_stateTimer >= stallDuration) ChangeState(DiveState.Chasing);
                    break;
            }

            if (_currentState == DiveState.Chasing || _currentState == DiveState.Locking)
            {
                ApplyMovement();
            }
        }

        private void UpdateIndicatorRotation()
        {
            if (directionRoot != null && directionRoot.gameObject.activeSelf)
            {
                Vector2 dir = (_diveTarget - (Vector2)transform.position).normalized;
                if (dir != Vector2.zero)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    directionRoot.rotation = Quaternion.Lerp(directionRoot.rotation, Quaternion.Euler(0, 0, angle - 90f), 15f * Time.deltaTime);
                }
            }
        }

        private Vector2 GetClampedViewportPosition(Vector2 worldPos)
        {
            if (_mainCamera == null) return worldPos;
            Vector3 vpPos = _mainCamera.WorldToViewportPoint(worldPos);
            vpPos.x = Mathf.Clamp(vpPos.x, viewportMargin, 1f - viewportMargin);
            vpPos.y = Mathf.Clamp(vpPos.y, viewportMargin, 1f - viewportMargin);
            vpPos.z = Mathf.Abs(_mainCamera.transform.position.z); 
            Vector3 clampedWorldPos = _mainCamera.ViewportToWorldPoint(vpPos);
            return new Vector2(clampedWorldPos.x, clampedWorldPos.y);
        }

        private bool IsInsideViewport()
        {
            if (_mainCamera == null) return true;
            Vector3 vp = _mainCamera.WorldToViewportPoint(transform.position);
            return vp.x >= viewportMargin && vp.x <= 1f - viewportMargin && 
                   vp.y >= viewportMargin && vp.y <= 1f - viewportMargin;
        }

        private void ApplyMovement()
        {
            transform.position += (Vector3)_currentVelocity * Time.deltaTime;
        }

        private static Tweener TweenSpriteColor(SpriteRenderer spriteRenderer, Color targetColor, float duration)
        {
            return DOTween.To(
                () => spriteRenderer.color,
                value =>
                {
                    if (spriteRenderer != null) spriteRenderer.color = value;
                },
                targetColor,
                duration);
        }

        public void Stop() => _currentVelocity = Vector2.zero;
    }
}
