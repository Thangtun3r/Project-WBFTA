using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private ParticleSystem hitEffect; 

    // We no longer [SerializeField] this. We find it via code.
    private PlayerAttack _playerAttack;

    private void Awake()
    {
        // Automatically find the attack script on this object or a parent/child
        _playerAttack = GetComponentInParent<PlayerAttack>() ?? GetComponentInChildren<PlayerAttack>();

        if (_playerAttack == null)
        {
            Debug.LogError("PlayerVisual: Could not find PlayerAttack script to listen to!");
        }
    }

    private void OnEnable()
    {
        if (_playerAttack != null)
            _playerAttack.OnHitTarget += PlayHitVisuals;
    }

    private void OnDisable()
    {
        if (_playerAttack != null)
            _playerAttack.OnHitTarget -= PlayHitVisuals;
    }

    private void PlayHitVisuals(Vector2 hitPoint, float damageAmount)
    {
        if (hitEffect != null)
        {
            // Teleport the particle system to the 2D contact point
            hitEffect.transform.position = new Vector3(hitPoint.x, hitPoint.y, hitEffect.transform.position.z);
            
            hitEffect.Play();
        }
        
    }
}