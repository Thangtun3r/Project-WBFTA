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
            Vector2 moveInput = Vector2.zero;

            if (_moveAction != null && _moveAction.enabled)
            {
                Vector2 actionVal = _moveAction.ReadValue<Vector2>();
                if (actionVal.sqrMagnitude > 0f)
                {
                    moveInput = actionVal;
                }
            }

            if (moveInput == Vector2.zero)
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
                    if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
                    if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
                    if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
                    if (moveInput.sqrMagnitude > 1f) moveInput = moveInput.normalized;
                }
                else
                {
                    if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
                    if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
                    if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
                    if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
                    if (moveInput.sqrMagnitude > 1f) moveInput = moveInput.normalized;
                }
            }

            Vector2 mousePos = Input.mousePosition;
            Vector2 mouseMove = Vector2.zero;

            if (mousePos.x < edgeThreshold)
            {
                float normalizedDist = mousePos.x / edgeThreshold;
                mouseMove.x = -(1f - normalizedDist);
            }
            else if (mousePos.x > Screen.width - edgeThreshold)
            {
                float normalizedDist = (Screen.width - mousePos.x) / edgeThreshold;
                mouseMove.x = 1f - normalizedDist;
            }

            if (mousePos.y < edgeThreshold)
            {
                float normalizedDist = mousePos.y / edgeThreshold;
                mouseMove.y = -(1f - normalizedDist);
            }
            else if (mousePos.y > Screen.height - edgeThreshold)
            {
                float normalizedDist = (Screen.height - mousePos.y) / edgeThreshold;
                mouseMove.y = 1f - normalizedDist;
            }

            Vector2 totalInput = moveInput + mouseMove;
            if (totalInput.sqrMagnitude > 1f) totalInput = totalInput.normalized;

            Vector3 moveDirection = new Vector3(totalInput.x, totalInput.y, 0f) * moveSpeed;

            transform.position = Vector3.SmoothDamp(transform.position, transform.position + moveDirection, ref _velocity, dampTime);
        }
    }
}