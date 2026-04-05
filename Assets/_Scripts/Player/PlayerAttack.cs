using UnityEngine;

namespace _Scripts.Player
{
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private bool useTrigger = true;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!useTrigger) return;
            if (other == null) return;
            var damagable = other.GetComponent<IDamagable>() ?? other.GetComponentInParent<IDamagable>();
            if (damagable != null) damagable.TakeDamage(damageAmount);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (useTrigger) return;
            if (collision == null || collision.collider == null) return;
            var damagable = collision.collider.GetComponent<IDamagable>() ?? collision.collider.GetComponentInParent<IDamagable>();
            if (damagable != null) damagable.TakeDamage(damageAmount);
        }
    }
}
