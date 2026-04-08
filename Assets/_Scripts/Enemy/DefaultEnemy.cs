using UnityEngine;
using _Scripts.Enemy.Modules;

namespace _Scripts.Enemy
{
    public class DefaultEnemy : BaseEnemy
    {
        private EnemyVisuals _visuals;
        private EnemyFSM _fsm;
        private Collider2D _collider;

        [SerializeField] private float deathReturnDelay = 3f;

        protected override void Awake()
        {
            base.Awake();
            
            _visuals = GetComponentInChildren<EnemyVisuals>();
            _fsm = GetComponentInChildren<EnemyFSM>();
            _collider = GetComponent<Collider2D>();
            
            var movement = GetComponent<IMovement>();
            var sensor = GetComponent<ITargetSensor>();
            var attack = GetComponent<IEnemyAttack>();
            var patrol = GetComponent<IPatrolModule>();

            if (_fsm != null)
            {
                _fsm.Initialize(movement, sensor, attack, patrol);
            }
        }

        public override void TakeDamage(float damage)
        {
            if (currentHealth <= 0f) return;

            currentHealth -= damage;
            _visuals?.PlayHitEffects();

            if (currentHealth <= 0f) OnDeath();
        }

        protected override void OnDeath()
        {
            // Now DefaultEnemy orchestrates the death sequence:
            if (_collider != null) _collider.enabled = false;
            
            if (_visuals != null)
            {
                _visuals.PlayDeathEffects();
                _visuals.HideVisual();
            }

            // Tell the FSM to transition to the dead state (so it stops moving/attacking)
            _fsm?.QueueNextState(EnemyFSM.EnemyState.Dead);
            
            // Simple return-to-pool replacement
            Invoke(nameof(DeactivateObject), deathReturnDelay);
        }

        private void DeactivateObject()
        {
            gameObject.SetActive(false);
        }
    }
}