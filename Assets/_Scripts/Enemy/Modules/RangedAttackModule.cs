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
        private Coroutine _attackRoutine;
        private EnemyConfig _config;
        private EnemyVisuals _enemyVisuals;

        private void Awake()
        {
            _config = GetComponentInParent<BaseEnemy>()?.Config;
            _sensor = GetComponentInChildren<ITargetSensor>();
            _enemyVisuals = GetComponentInParent<EnemyVisuals>();
            rotationTarget = rotationTarget ?? transform;
            firePoint = firePoint ?? transform;
        }

        public void SetAttackActive(bool active)
        {
            if (active && _attackRoutine == null) _attackRoutine = StartCoroutine(AttackLoop());
            else if (!active && _attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
                ResetVisuals();
            }
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                // 1. Wait for target
                yield return new WaitUntil(() => _sensor != null && _sensor.HasTarget);

                // 2. Alignment Phase: Rotate until facing target (within 5 degrees)
                while (true)
                {
                    float angleDiff = GetAngleToTarget();
                    if (Mathf.Abs(angleDiff) < 5f) break; 

                    ApplyRotation(angleDiff);
                    yield return null;
                }

                // 3. Windup Phase: Start shaking now that we are aligned
                _enemyVisuals?.OnFlashWindupStart?.Invoke();
                transform.DOLocalRotate(shakeAngle, 0.05f).SetLoops(-1, LoopType.Yoyo);
                
                float elapsed = 0;
                while (elapsed < windupDuration)
                {
                    ApplyRotation(GetAngleToTarget()); // Keep tracking during windup
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // 4. Fire & Cooldown
                ExecuteFire();
                yield return new WaitForSeconds(cooldown);
            }
        }

        private void ExecuteFire()
        {
            ResetVisuals();
            
            Vector3 dir = (rotationTarget.right); // Use current forward after alignment
            float damage = _config != null ? _config.damage : 10f;

            ProjectilePool.OnProjectileRequested?.Invoke(new ProjectileRequest(
                firePoint.position, rotationTarget.rotation, damage, dir * projSpeed));

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
        }

        public bool CanHit() => _attackRoutine != null;
        public void StartHitCooldown() { }
    }
}