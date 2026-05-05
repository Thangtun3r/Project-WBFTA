using UnityEngine;

public class DragableCursor : MonoBehaviour
{
    [Header("Draggable Settings")]
    [SerializeField] private float throwForceMultiplier = 10f;
    [SerializeField] private float throwSpeed = 1f;
    [SerializeField] private LayerMask dragMask;
    [SerializeField] private float detectionRadius = 0.5f;

    [Header("Throw Force Limits")]
    [SerializeField] private float minThrowForce = 1f;
    [SerializeField] private float maxThrowForce = 20f;

    private IDragable selectedDragable = null;
    public Rigidbody2D selectedRb = null;
    private Vector2 dragOffset = Vector2.zero;
    private Vector2 lastCursorWorldPos;
    private Vector2 lastCursorScreenPos;
    private Camera mainCamera;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private GameObject cursorGameObject;

    private void Awake()
    {
        mainCamera = Camera.main;
        // Try to find the player transform
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTransform = playerGO.transform;
            playerRb = playerGO.GetComponent<Rigidbody2D>();
        }
        else
        {
            playerTransform = transform; // Fallback to self if player not found
            playerRb = GetComponent<Rigidbody2D>();
        }
        
        // Find the cursor GameObject
        MouseFollower mouseFollower = FindObjectOfType<MouseFollower>();
        if (mouseFollower != null && mouseFollower.cursor != null)
            cursorGameObject = mouseFollower.cursor;
    }

    private void Update()
    {
        Vector2 cursorScreenPos = cursorGameObject != null
            ? (Vector2)mainCamera.WorldToScreenPoint(cursorGameObject.transform.position)
            : (Vector2)Input.mousePosition;

        Vector2 cursorWorldPos = cursorGameObject != null
            ? (Vector2)cursorGameObject.transform.position
            : mainCamera.ScreenToWorldPoint(cursorScreenPos);

        if (Input.GetMouseButtonDown(0))
            TryGrabObject(cursorWorldPos, cursorScreenPos);

        if (Input.GetMouseButton(0) && selectedDragable != null)
            DragObject(cursorWorldPos, cursorScreenPos);

        if (Input.GetMouseButtonUp(0) && selectedDragable != null)
                ReleaseObject(cursorWorldPos, cursorScreenPos);
    }

            private void TryGrabObject(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        // Use OverlapCircle to find objects at cursor position
        Collider2D[] hits = Physics2D.OverlapCircleAll(cursorWorldPos, detectionRadius, dragMask);
        
        if (hits.Length == 0) return;

        // If multiple objects overlap, get the topmost one (highest Z position)
        Collider2D topCollider = hits[0];
        for (int i = 1; i < hits.Length; i++)
        {
            if (hits[i].transform.position.z > topCollider.transform.position.z)
                topCollider = hits[i];
        }

        selectedDragable = topCollider.GetComponent<IDragable>();
        if (selectedDragable == null) return;

        selectedRb = selectedDragable.GetRigidbody();
        dragOffset = (Vector2)selectedDragable.GetTransform().position - cursorWorldPos;
        lastCursorWorldPos = cursorWorldPos;
        lastCursorScreenPos = cursorScreenPos;
        selectedDragable.OnStartDrag();
    }

    private void DragObject(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        selectedRb.MovePosition(cursorWorldPos + dragOffset);
        selectedDragable.OnDrag(cursorWorldPos + dragOffset);
        lastCursorWorldPos = cursorWorldPos;
        lastCursorScreenPos = cursorScreenPos;
    }

    private void ReleaseObject(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        Vector2 screenDelta = cursorScreenPos - lastCursorScreenPos;
        // Convert screen pixels to world units using camera orthographic size
        float pixelsPerUnit = Screen.height / (mainCamera.orthographicSize * 2f);
        Vector2 worldDelta = screenDelta / pixelsPerUnit;
        Vector2 throwVelocity = worldDelta / Time.deltaTime;
        
        // Subtract player velocity so throw is relative to player movement
        if (playerRb != null)
            throwVelocity -= playerRb.linearVelocity;
        
        Vector2 impulse = throwVelocity * throwForceMultiplier * throwSpeed;

        float clampedMagnitude = Mathf.Clamp(impulse.magnitude, minThrowForce, maxThrowForce);
        if (impulse != Vector2.zero)
            impulse = impulse.normalized * clampedMagnitude;

        selectedDragable.OnEndDrag(impulse);
        if (selectedRb != null)
        {
            selectedRb.linearVelocity = impulse;
        }

        selectedDragable = null;
        selectedRb = null;
    }

    /// <summary>
    /// Forcefully release the object if we are in the middle of a drag.
    /// Pass lastCursorWorldPos so the release velocity is zero (a clean drop).
    /// </summary>
    public void ForceReleaseIfDragging()
    {
        if (selectedDragable != null)
            ReleaseObject(lastCursorWorldPos, lastCursorScreenPos);
    }
}