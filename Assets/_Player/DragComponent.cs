using UnityEngine;
using System.Collections;

/// <summary>
/// Drop this component onto any enemy prefab to make it draggable.
/// Requires the enemy to have a Rigidbody2D.
/// Works with DragableCursor out of the box via IDragable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DragComponent : MonoBehaviour, IDragable
{
    [Header("Visuals")]
    [SerializeField] private GameObject visualContainer;
    [SerializeField] private float wiggleAmount = 5f;
    [SerializeField] private float wiggleSpeed = 10f;
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float scaleSpeed = 0.2f;

    [Header("Throw Settings")]
    [SerializeField] private float throwForceMultiplier = 10f;

    private Rigidbody2D _rb; 
    private Collider2D _col;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _originalScale;
    private Coroutine _wiggleCoroutine;

    // Optional: cache the enemy FSM if present so we can pause it during drag
    private _Scripts.Enemy.EnemyFSM _fsm;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _rb.AddForce(Vector2.up * throwForceMultiplier, ForceMode2D.Impulse);   
        }
    }

    // -------------------------------------------------------------------------
    // IDragable
    // -------------------------------------------------------------------------

    public void OnStartDrag()
    {
    }

    public void OnDrag(Vector2 position)
    {
        
    }

    public void OnEndDrag(Vector2 velocity)
    {
        if (_rb != null)
        {
            //Debug.Log($"Applying throw velocity: {velocity}");
            _rb.AddForce(velocity * throwForceMultiplier, ForceMode2D.Impulse);
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