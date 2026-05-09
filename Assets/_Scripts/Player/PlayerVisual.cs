using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("VFX Settings")]
    private string hitEffectName = "playerHit";    [SerializeField] private string enemyHitEffectName = "enemyHit";
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
        
        _Scripts.Enemy.BaseEnemy.OnAnyEnemyHit += OnEnemyHitResponse;
    }

    private void OnDisable()
    {
        if (_playerAttack != null)
            _playerAttack.OnHitTarget -= PlayHitVisuals;
        
        _Scripts.Enemy.BaseEnemy.OnAnyEnemyHit -= OnEnemyHitResponse;
    }

    private void PlayHitVisuals(Vector2 hitPoint, float damageAmount, bool isCrit)
    {
        VFXStation.PlayEffect(hitEffectName, new Vector3(hitPoint.x, hitPoint.y, 0));
    }

    private void OnEnemyHitResponse(_Scripts.Enemy.BaseEnemy enemy)
    {
        VFXStation.PlayEffect(enemyHitEffectName, enemy.transform.position);
    }
}