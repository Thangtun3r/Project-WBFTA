using UnityEngine;
using System.Collections;

namespace _Scripts.Enemy
{
    public class EnemyFSM : global::StateMachine<EnemyFSM.EnemyState>
    {
        public enum EnemyState { Patrol, Chase, Attack, Dead }

        public EnemyConfig Config;
        [HideInInspector] public IEnemyAttack attackModule;
        [HideInInspector] public EnemyVisuals visuals;
        [HideInInspector] public Collider2D enemyCollider;
        [HideInInspector] public Transform player;
        [HideInInspector] public Vector2 CurrentVelocity;

        // Public properties for state access
        public IEnemyAttack AttackModule => attackModule;
        public EnemyVisuals Visuals => visuals;
        public Collider2D EnemyCollider => enemyCollider;

        // Configurable delays and buffers
        [SerializeField] private float attackExitBuffer = 0.2f;
        [SerializeField] private float deathReturnDelay = 3f;

        public float AttackExitBuffer => attackExitBuffer;
        public float DeathReturnDelay => deathReturnDelay;

        protected void Awake()
        {
            visuals = GetComponent<EnemyVisuals>();
            attackModule = GetComponent<IEnemyAttack>();
            enemyCollider = GetComponent<Collider2D>();
            player = GameObject.FindWithTag("Player")?.transform;

            // Modular State Registration
            States.Add(EnemyState.Patrol, new PatrolState(this));  // ← Add this
            States.Add(EnemyState.Chase, new ChaseState(this));
            States.Add(EnemyState.Attack, new AttackState(this));
            States.Add(EnemyState.Dead, new DeadState(this));

            CurrentState = States[EnemyState.Patrol];  // ← Start in Patrol, not Chase
        }
        public void ScheduleReturnToPool(float delay)
        {
            StartCoroutine(ReturnToPoolCoroutine(delay));
        }

        private IEnumerator ReturnToPoolCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Return to object pool or destroy
            gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            if (Config == null) return;

            // Draw detection range
            Gizmos.color = Color.yellow;
            DrawCircleGizmo(transform.position, Config.detectionRange, 32);

            // Draw attack range
            Gizmos.color = Color.red;
            DrawCircleGizmo(transform.position, Config.attackRange, 32);

            // Draw stop distance
            Gizmos.color = Color.green;
            DrawCircleGizmo(transform.position, Config.stopDistance, 16);

            // Draw patrol reach (360-degree patrol with 5 unit reach)
            Gizmos.color = Color.cyan;
            DrawCircleGizmo(transform.position, 5f, 32);

            // Draw enemy avoidance radius
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            DrawCircleGizmo(transform.position, 2f, 16);
        }

        private void DrawCircleGizmo(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}