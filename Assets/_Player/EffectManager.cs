using UnityEngine;
using _Scripts.Enemy;
using _Scripts.Enemy.Modules;

/// <summary>
/// Communicator port between the enemy and DragableCursor.
/// Implements IDragable as a simple data container.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EffectManager : MonoBehaviour, IDragable
{
    public bool WasThrown { get; set; }

    private Rigidbody2D _rb; 
    private BaseEnemy _baseEnemy;
    private EnemyFSM _enemyFSM;
    private EnemyStatusController _status;
    private EnemyVisual _visuals;
    private IMovement _movement;
    private IEnemyAttack[] _attackModules;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _baseEnemy = GetComponentInParent<BaseEnemy>();
        
        if (_baseEnemy != null)
        {
            _enemyFSM = _baseEnemy.GetComponentInChildren<EnemyFSM>();
        }
        else
        {
            _enemyFSM = GetComponentInChildren<EnemyFSM>();
        }

        if (_baseEnemy != null)
        {
            _status = EnemyStatusController.FindFor(_baseEnemy);
            _visuals = _baseEnemy.GetComponentInChildren<EnemyVisual>();
            _movement = _baseEnemy.GetComponentInChildren<IMovement>();
            _attackModules = _baseEnemy.GetComponentsInChildren<IEnemyAttack>();
        }
        else
        {
            _status = EnemyStatusController.FindFor(this);
            _visuals = GetComponentInParent<EnemyVisual>();
            _movement = GetComponentInParent<IMovement>();
            _attackModules = GetComponentsInParent<IEnemyAttack>();
        }
    }

    // -------------------------------------------------------------------------
    // IDragable
    // -------------------------------------------------------------------------

    public void OnStartDrag()
    {
        WasThrown = false;
        _status?.ApplyStatus(EnemyStatusType.Grabbed);
        _visuals?.PlayShake();
        _movement?.Stop();

        for (int i = 0; i < _attackModules.Length; i++)
        {
            _attackModules[i]?.SetAttackActive(false);
        }
    }

    public void OnDrag(Vector2 position)
    {
    }

    public void OnEndDrag(Vector2 velocity)
    {
        WasThrown = velocity.magnitude > 0.1f;
        _status?.RemoveStatus(EnemyStatusType.Grabbed);
        _enemyFSM?.ReenterCurrentState();
    }

    public Rigidbody2D GetRigidbody()
    {
        return _rb;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
