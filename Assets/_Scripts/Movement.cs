using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dampTime = 0.1f;

    private InputAction _moveAction;
    private Vector3 _velocity = Vector3.zero;

    private void Awake()
    {
        // Find the "Move" action
        _moveAction = InputSystem.actions.FindAction("Move");

        if (_moveAction == null)
        {
            Debug.LogError("Movement Error: 'Move' action not found! Check your Action Map names.");
        }
        else
        {
            Debug.Log("Movement: 'Move' action successfully linked.");
        }
    }

    private void OnEnable()
    {
        if (_moveAction != null)
        {
            _moveAction.Enable();
            Debug.Log("Movement: Move Action ENABLED.");
        }
    }

    private void OnDisable()
    {
        if (_moveAction != null)
        {
            _moveAction.Disable();
            Debug.Log("Movement: Move Action DISABLED.");
        }
    }

    private void Update()
    {
        if (_moveAction == null) return;

        // Read the Vector2 (X = A/D, Y = W/S)
        Vector2 moveValue = _moveAction.ReadValue<Vector2>();

        // Calculate target position
        Vector3 moveDirection = new Vector3(moveValue.x, moveValue.y, 0f) * moveSpeed;
        
        // Apply damped movement
        transform.position = Vector3.SmoothDamp(transform.position, transform.position + moveDirection, ref _velocity, dampTime);
    }
}