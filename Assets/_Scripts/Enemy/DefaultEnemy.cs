using System.Collections;
using UnityEngine;

namespace _Scripts.Enemy
{
    public class DefaultEnemy : BaseEnemy
    {
        [SerializeField] private SpriteRenderer enemyVisual;
        [SerializeField] private float _health;

        private Coroutine _flashCoroutine;

        private void Start()
        {
            _health = 100f;
        }
        
        public override void TakeDamage(float damage)
        {
            if (enemyVisual != null)
            {
                Color currentDefault = enemyVisual.color;
                if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); _flashCoroutine = null; }
                _flashCoroutine = StartCoroutine(FlashWhiteRoutine(0.15f, currentDefault));
            }
            _health -= damage;
            if (_health <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashWhiteRoutine(float duration, Color originalColor)
        {
            enemyVisual.color = Color.white;
            yield return new WaitForSeconds(duration);
            enemyVisual.color = originalColor;
            _flashCoroutine = null;
        }
    }
}
