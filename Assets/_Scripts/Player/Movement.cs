using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public class Movement : MonoBehaviour
    {
        public enum MovementMode
        {
            Wasd,
            Mouse,
            Both
        }

        [Header("Targeting")]
        [Tooltip("The object that will actually move. If empty, this script moves itself.")]
        [SerializeField] private Transform targetObject;

        [Header("Settings")]
        [SerializeField] private MovementMode movementMode = MovementMode.Both;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private bool useDamping = true;
        [SerializeField] private float dampTime = 0.1f;
        [SerializeField] private float edgeThreshold = 50f;

        private InputAction _moveAction;
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _velocityDamp = Vector3.zero;
        private MouseFollower _mouseFollower;

        private void Awake()
        {
            // Fallback: If no target is assigned, move the object this script is on
            if (targetObject == null)
            {
                targetObject = transform;
            }

            // Find the "Move" action
            _moveAction = InputSystem.actions.FindAction("Move");

            if (_moveAction == null)
            {
                Debug.LogError("Movement Error: 'Move' action not found! Check your Action Map names.");
            }

            _mouseFollower = FindFirstObjectByType<MouseFollower>();
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
        }

        public void AddSpeedMultiplier(float amount)
        {
            speedMultiplier += amount;
        }

        private void Update()
        {
            Vector2 combinedInput = CalculateInput();

            Vector3 targetVelocity = new Vector3(combinedInput.x, combinedInput.y, 0f) * (moveSpeed * speedMultiplier);
            if (useDamping && dampTime > 0f)
            {
                _currentVelocity = Vector3.SmoothDamp(_currentVelocity, targetVelocity, ref _velocityDamp, dampTime);
            }
            else
            {
                _currentVelocity = targetVelocity;
                _velocityDamp = Vector3.zero;
            }

            targetObject.position += _currentVelocity * Time.deltaTime;
        }
        
         private Vector2 CalculateInput()
        {
            Vector2 moveInput = Vector2.zero;

            // 1. WASD / Input System Logic
            if (movementMode == MovementMode.Wasd || movementMode == MovementMode.Both)
            {
                if (_moveAction != null && _moveAction.enabled)
                {
                    Vector2 actionVal = _moveAction.ReadValue<Vector2>();
                    if (actionVal.sqrMagnitude > 0f)
                    {
                        moveInput = actionVal;
                    }
                }

                // Hardcoded keyboard fallback if Action Map isn't triggering
                if (moveInput == Vector2.zero)
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
                        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
                        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
                        if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
                    }
                    else
                    {
                        if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
                        if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
                        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
                        if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
                    }
                    
                    if (moveInput.sqrMagnitude > 1f) moveInput = moveInput.normalized;
                }
            }

            // 2. Mouse Edge Logic
            Vector2 mouseMove = Vector2.zero;
            if (movementMode == MovementMode.Mouse || movementMode == MovementMode.Both)
            {
                Vector2 mousePos = _mouseFollower != null
                    ? _mouseFollower.GetVirtualScreenPosForFrame()
                    : (Vector2)Input.mousePosition;

                // X Axis
                if (mousePos.x < edgeThreshold)
                {
                    mouseMove.x = -1f;
                }
                else if (mousePos.x > Screen.width - edgeThreshold)
                {
                    mouseMove.x = 1f;
                }

                // Y Axis
                if (mousePos.y < edgeThreshold)
                {
                    mouseMove.y = -1f;
                }
                else if (mousePos.y > Screen.height - edgeThreshold)
                {
                    mouseMove.y = 1f;
                }
            }

            Vector2 total = moveInput + mouseMove;
            return total.sqrMagnitude > 1f ? total.normalized : total;
        }
    }
       
}
