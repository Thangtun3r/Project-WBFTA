using UnityEngine;
using DG.Tweening;

namespace _Scripts.Enemy.Modules
{
    public class RangedAttackModule : MonoBehaviour, IEnemyAttack
    {
        [Header("Projectile Setup")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform visualTarget;
        [SerializeField] private Transform rotationTarget; // The object to rotate toward the player
        
        [Header("Attack Settings")]
        [SerializeField] private float attackCooldown = 1.5f; // Time between attacks
        [SerializeField] private float projectileSpeed = 10f;

        [Header("Windup Settings")]
        [SerializeField] private Vector3 shakeAngle = new Vector3(0, 0, 15f);
        [SerializeField] private float shakeSpeed = 0.05f;
        [SerializeField] private Ease shakeEase = Ease.InOutSine;
        [SerializeField] private float rotationSpeed = 5f;
        
        private EnemyConfig _config;
        private ITargetSensor _targetSensor;
        private bool _isAttacking;
        private float _nextFireTime;
        
        private enum AttackState { Cooldown, WindingUp }
        private AttackState _attackState = AttackState.Cooldown;
        private float _windupTimer;

        private void Awake()
        {
            _config = GetComponentInParent<BaseEnemy>()?.Config;
            
            // Try to find the sensor on the same GameObject or children
            _targetSensor = GetComponentInChildren<ITargetSensor>();
            
            if (firePoint == null)
            {
                firePoint = transform; // Default to this transform if not set
            }
            
            if (visualTarget == null)
            {
                visualTarget = transform; // Default to this transform if not set
                Debug.LogWarning("RangedAttackModule: visualTarget not assigned, using this transform");
            }
            
            if (rotationTarget == null)
            {
                rotationTarget = transform; // Default to this transform if not set
                Debug.LogWarning("RangedAttackModule: rotationTarget not assigned, using this transform");
            }
        }

        public void SetAttackActive(bool active)
        {
            _isAttacking = active;
            
            if (!active && _attackState == AttackState.WindingUp)
            {
                StopWindup();
            }
            else if (active)
            {
                // Optional: shoot immediately when entering the state
                // _nextFireTime = Time.time; 
            }
        }

        public bool CanHit()
        {
            return _isAttacking;
        }

        public void StartHitCooldown()
        {
            // Ranged attack does not use hitbox cooldowns.
        }

        private void Update()
        {
            if (!_isAttacking || _targetSensor == null || !_targetSensor.HasTarget) return;

            float actualWindupDuration = attackCooldown * 0.1f;
            float cooldownDuration = attackCooldown * 0.9f;

            if (_attackState == AttackState.Cooldown)
            {
                if (Time.time >= _nextFireTime)
                {
                    StartWindup();
                }
            }
            else if (_attackState == AttackState.WindingUp)
            {
                // Rotate smoothly towards the target we're shooting at using Lerp
                Vector3 direction = (_targetSensor.TargetPosition - rotationTarget.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                rotationTarget.rotation = Quaternion.Lerp(rotationTarget.rotation, targetRotation, Time.deltaTime * rotationSpeed * 5f);

                _windupTimer += Time.deltaTime;
                if (_windupTimer >= actualWindupDuration)
                {
                    // Snap to perfectly align at the exact moment of the shot
                    rotationTarget.rotation = targetRotation;

                    StopWindup();
                    Shoot(_targetSensor.TargetPosition);
                    _nextFireTime = Time.time + cooldownDuration;
                }
            }
        }

        private void StartWindup()
        {
            _attackState = AttackState.WindingUp;
            _windupTimer = 0f;
            
            if (visualTarget != null)
            {
                visualTarget.DOKill(true);
                visualTarget.localRotation = Quaternion.Euler(-shakeAngle);
                visualTarget.DOLocalRotate(shakeAngle, shakeSpeed)
                    .SetEase(shakeEase)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void StopWindup()
        {
            _attackState = AttackState.Cooldown;
            if (visualTarget != null)
            {
                visualTarget.DOKill(true);
                visualTarget.localRotation = Quaternion.identity;
            }
        }

        private void Shoot(Vector3 targetPosition)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("No Projectile Prefab assigned in RangedAttackModule!");
                return;
            }

            // Spawn the projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            if (visualTarget != null)
            {
                visualTarget.DOKill(true);
                visualTarget.localScale = Vector3.one;
                visualTarget.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
            }
            
            // Calculate direction to the target
            Vector3 direction = (targetPosition - firePoint.position).normalized;
            
            // Rotate the projectile to face the target (assuming standard 2D rotation)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // Apply velocity to the 2D Rigidbody
            if (projectile.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
            else
            {
                Debug.LogWarning("Projectile prefab is missing a Rigidbody2D!");
            }            
            // Pass the damage to the projectile
            if (projectile.TryGetComponent<Projectile>(out Projectile projScript))
            {
                float damage = _config != null ? _config.damage : 10f;
                projScript.Initialize(damage);
            }        }
    }
}