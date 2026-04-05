using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public class Movement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dampTime = 0.1f;
        [SerializeField] private float edgeThreshold = 50f;

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
            // Mouse edge detection with proportional movement
            Vector2 mousePos = Input.mousePosition;
            Vector2 moveInput = Vector2.zero;

            // Left edge
            if (mousePos.x < edgeThreshold)
            {
                float normalizedDist = mousePos.x / edgeThreshold;
                moveInput.x = -(1f - normalizedDist);
            }
            // Right edge
            else if (mousePos.x > Screen.width - edgeThreshold)
            {
                float normalizedDist = (Screen.width - mousePos.x) / edgeThreshold;
                moveInput.x = 1f - normalizedDist;
            }

            // Bottom edge
            if (mousePos.y < edgeThreshold)
            {
                float normalizedDist = mousePos.y / edgeThreshold;
                moveInput.y = -(1f - normalizedDist);
            }
            // Top edge
            else if (mousePos.y > Screen.height - edgeThreshold)
            {
                float normalizedDist = (Screen.height - mousePos.y) / edgeThreshold;
                moveInput.y = 1f - normalizedDist;
            }

            // Calculate target position
            Vector3 moveDirection = new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed;
            
            // Apply damped movement
            transform.position = Vector3.SmoothDamp(transform.position, transform.position + moveDirection, ref _velocity, dampTime);
        }
    }
}