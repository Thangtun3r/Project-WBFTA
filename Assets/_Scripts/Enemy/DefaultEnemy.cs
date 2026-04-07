using UnityEngine;

namespace _Scripts.Enemy
{
    public class DefaultEnemy : BaseEnemy
    {
        private EnemyVisuals _visuals;
        private EnemyFSM _fsm;

        protected override void Awake()
        {
            base.Awake();
            _visuals = GetComponent<EnemyVisuals>();
            _fsm = GetComponent<EnemyFSM>();
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
            // The ONLY logic here is triggering the state change.
            // The DeadState class handles the rest.
            _fsm.QueueNextState(EnemyFSM.EnemyState.Dead);
        }
    }
}