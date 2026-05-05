using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class DragImpactDamage : MonoBehaviour
{
    [Header("Debug Options")]
    [SerializeField] private bool logImpact = true;
    [SerializeField] private bool drawImpactRay = true;
    [SerializeField] private Color impactColor = Color.red;
    [SerializeField] private float debugLineDuration = 0.5f;

    [Header("Impact Settings")]
    [SerializeField] private float impactThreshold = 2.0f;
    [SerializeField] private LayerMask impactLayer;

    public static event Action OnImpactDetected;

    private Rigidbody2D _rb;
    private PlayerStatMachine _playerStats;
    private DragComponent _dragComponent;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _dragComponent = GetComponent<DragComponent>();
        
        // Find the player's Stat Machine in the scene to get their current damage
        _playerStats = FindObjectOfType<PlayerStatMachine>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Double check this object is currently flagged as "thrown" by the player
        if (_dragComponent != null && !_dragComponent.WasThrown)
            return;

        // Check if the collision object is in the specified layer
        if (((1 << collision.gameObject.layer) & impactLayer) != 0)
        {
            // Calculate impact power based on velocity and mass
            float impactPower = collision.relativeVelocity.magnitude * _rb.mass;

            // Only trigger if impact is above the threshold
            if (impactPower > impactThreshold)
            {
                OnImpactDetected?.Invoke();

                if (logImpact)
                {
                    Debug.Log($"Impact Detected! Object: {collision.gameObject.name}, Impact Power: {impactPower}");
                }

                if (drawImpactRay)
                {
                    Vector2 impactDirection = -collision.contacts[0].normal;
                    Debug.DrawRay(
                        collision.contacts[0].point, 
                        impactDirection * impactPower * 0.1f, 
                        impactColor, 
                        debugLineDuration
                    );
                }

                // Get the damage amount from the player's stat machine
                float damageToDeal = 0f;
                if (_playerStats != null)
                {
                    // This will also account for crit calculations if applicable
                    damageToDeal = _playerStats.GetCalculatedAttackDamage(); 
                }

                // Deal damage to the object we hit
                IDamagable damagable = collision.gameObject.GetComponent<IDamagable>();
                if (damagable != null && damageToDeal > 0)
                {
                    damagable.TakeDamage(damageToDeal);
                }
            }
        }
    }
}