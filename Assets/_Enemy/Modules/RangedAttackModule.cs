using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace _Scripts.Enemy.Modules
{
    public class RangedAttackModule : MonoBehaviour, IEnemyAttack
    {
        [Header("Settings")]
        [SerializeField] private float cooldown = 1.2f;
        [SerializeField] private float windupDuration = 0.4f;
        [SerializeField] private float projSpeed = 10f;
        [SerializeField] private float turnSpeed = 10f;

        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform rotationTarget;
        [SerializeField] private Vector3 shakeAngle = new Vector3(0, 0, 15f);

        private ITargetSensor _sensor;
        private EnemyStatusController _status;
        private Coroutine _attackRoutine;
        private EnemyConfig _config;
        private EnemyVisual _enemyVisuals;

        private void Awake()
        {
            _config = GetComponentInParent<BaseEnemy>()?.Config;
            _sensor = GetComponentInChildren<ITargetSensor>();
            _status = EnemyStatusController.FindFor(this);
            _enemyVisuals = GetComponentInParent<EnemyVisual>();
            rotationTarget = rotationTarget ?? transform;
            firePoint = firePoint ?? transform;
        }

        public void SetAttackActive(bool active)
        {
            if (active && _status != null && !_status.CanAttack)
                return;

            if (active && _attackRoutine == null) _attackRoutine = StartCoroutine(AttackLoop());
            else if (!active && _attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
                ResetVisuals();
            }
        }

        private void OnDisable()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }
            ResetVisuals();
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                if (_status != null && !_status.CanAttack)
                {
                    ResetVisuals();
                    yield return null;
                    continue;
                }

                // 1. Wait for target
                yield return new WaitUntil(() => _sensor != null && _sensor.HasTarget);

                // 2. Alignment Phase: Rotate until facing target (within 5 degrees)
                while (true)
                {
                    if (_status != null && !_status.CanAttack)
                        break;

                    float angleDiff = GetAngleToTarget();
                    if (Mathf.Abs(angleDiff) < 5f) break; 

                    ApplyRotation(angleDiff);
                    yield return null;
                }

        
                transform.DOLocalRotate(shakeAngle, 0.05f).SetLoops(-1, LoopType.Yoyo);
                
                float elapsed = 0;
                while (elapsed < windupDuration)
                {
                    if (_status != null && !_status.CanAttack)
                        break;

                    ApplyRotation(GetAngleToTarget()); // Keep tracking during windup
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (_status != null && !_status.CanAttack)
                    continue;

                // 4. Fire & Cooldown
                ExecuteFire();
                yield return new WaitForSeconds(cooldown);
            }
        }

    private void ExecuteFire()
    {
        ResetVisuals();
        
        // 1. Prepare basic data
        Vector3 dir = rotationTarget.right; 
        float damage = _config != null ? _config.damage : 10f;

        // 2. Create the Request
        // Note: We use the object initializer {} to fill fields easily
        ProjectileRequest request = new ProjectileRequest
        {
            ProjectileID = "DefaultBullet", // Match the ID in your Pool Library!
            Position = firePoint.position,
            Rotation = rotationTarget.rotation,
            Direction = dir * projSpeed,
            Damage = damage,
        };

        // 3. Call the Pool directly (using the Singleton Instance)
        ProjectilePool.Instance.RequestProjectile(request);

        // Visual feedback
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f);
    }








        private float GetAngleToTarget()
        {
            Vector3 dir = (_sensor.TargetPosition - rotationTarget.position).normalized;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(rotationTarget.eulerAngles.z, targetAngle);
        }

        private void ApplyRotation(float angleDiff)
        {
            float rotationStep = angleDiff * Time.deltaTime * turnSpeed;
            rotationTarget.Rotate(0, 0, rotationStep);
        }

        private void ResetVisuals()
        {
            transform.DOKill();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public bool CanHit() => (_status == null || _status.CanAttack) && _attackRoutine != null;
        public void StartHitCooldown() { }
    }
}
