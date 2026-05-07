using UnityEngine;
using _Scripts.Enemy;

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
    }

    // -------------------------------------------------------------------------
    // IDragable
    // -------------------------------------------------------------------------

    public void OnStartDrag()
    {
        WasThrown = false;
        if (_enemyFSM != null)
        {
            _enemyFSM.enabled = false;
        }
    }

    public void OnDrag(Vector2 position)
    {
    }

    public void OnEndDrag(Vector2 velocity)
    {
        WasThrown = velocity.magnitude > 0.1f;
        if (_enemyFSM != null)
        {
            _enemyFSM.enabled = true;
        }
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