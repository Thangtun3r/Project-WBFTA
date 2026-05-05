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
    private float wiggleAmount = 5f;
    private float wiggleSpeed = 30f;
    private float scaleMultiplier = 1.1f;
    private float scaleSpeed = 0.2f;

    [Header("Throw Settings")]
    [SerializeField] private float throwForceMultiplier = 10f;
    [SerializeField] private float damageFalloffVelocity = 1f;

    public bool WasThrown { get; private set; }

    private Rigidbody2D _rb; 
    private Collider2D _col;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _originalScale;
    private Coroutine _wiggleCoroutine;
    private Coroutine _scaleCoroutine;
    private Transform _visualTransform;

    // Optional: cache the enemy FSM if present so we can pause it during drag
    private _Scripts.Enemy.EnemyFSM _fsm;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _visualTransform = visualContainer != null ? visualContainer.transform : transform;
        _originalScale = _visualTransform.localScale;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _rb.AddForce(Vector2.up * throwForceMultiplier, ForceMode2D.Impulse);   
        }
        
        if (WasThrown && _rb != null && _rb.linearVelocity.magnitude <= damageFalloffVelocity)
        {
            WasThrown = false;
        }
    }

    // -------------------------------------------------------------------------
    // IDragable
    // -------------------------------------------------------------------------

    public void OnStartDrag()
    {
        WasThrown = false;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.isKinematic = true;
        }

        if (_col != null)
            _col.isTrigger = true;

        StartWiggle();
        StartScale(_originalScale * scaleMultiplier);
    }

    public void OnDrag(Vector2 position)
    {
        
    }

    public void OnEndDrag(Vector2 velocity)
    {
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.AddForce(velocity * throwForceMultiplier, ForceMode2D.Impulse);
        }

        if (velocity.magnitude > 0.1f)
        {
            WasThrown = true;
        }

        if (_col != null)
            _col.isTrigger = false;

        StopWiggle();
        StartScale(_originalScale);
    }

    public Rigidbody2D GetRigidbody()
    {
        return _rb;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    private void StartWiggle()
    {
        StopWiggle();
        _wiggleCoroutine = StartCoroutine(WiggleEffect());
    }

    private void StopWiggle()
    {
        if (_wiggleCoroutine != null)
        {
            StopCoroutine(_wiggleCoroutine);
            _wiggleCoroutine = null;
        }

        if (_visualTransform != null)
            _visualTransform.localRotation = Quaternion.identity;
    }

    private void StartScale(Vector3 targetScale)
    {
        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        _scaleCoroutine = StartCoroutine(ScaleEffect(_visualTransform, targetScale));
    }

    private IEnumerator WiggleEffect()
    {
        float t = 0f;
        while (true)
        {
            float angle = Mathf.Sin(t * wiggleSpeed) * wiggleAmount;
            _visualTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ScaleEffect(Transform target, Vector3 targetScale)
    {
        float elapsed = 0f;
        Vector3 start = target.localScale;
        while (elapsed < scaleSpeed)
        {
            target.localScale = Vector3.Lerp(start, targetScale, elapsed / scaleSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = targetScale;
    }
}