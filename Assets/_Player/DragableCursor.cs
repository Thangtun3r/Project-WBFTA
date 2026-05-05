using UnityEngine;

public class DragableCursor : MonoBehaviour
{
    [Header("Draggable Settings")]
    [SerializeField] private float throwSensitivity = 1f;
    [SerializeField] private LayerMask dragMask;
    [SerializeField] private float detectionRadius = 0.5f;

    [Header("Throw Force Limits")]
    [SerializeField] private float minThrowForce = 1f;
    [SerializeField] private float maxThrowForce = 20f;
    [SerializeField] private float minThrowDistance = 10f;

    private IDragable selectedDragable;
    private Rigidbody2D selectedRb;
    private Vector2 dragOffset;
    private Vector2 lastCursorScreenPos;
    private Vector2 cursorVelocity;
    private Camera mainCamera;
    private MouseFollower mouseFollower;

    private void Awake()
    {
        mainCamera = Camera.main;
        mouseFollower = FindObjectOfType<MouseFollower>();
    }

    private void Update()
    {
        Vector2 cursorScreenPos = GetCursorScreenPos();
        Vector2 cursorWorldPos = GetCursorWorldPos(cursorScreenPos);

        if (Input.GetMouseButtonDown(0))
            TryBeginDrag(cursorWorldPos, cursorScreenPos);

        if (Input.GetMouseButton(0) && selectedDragable != null)
            UpdateDrag(cursorWorldPos, cursorScreenPos);

        if (Input.GetMouseButtonUp(0) && selectedDragable != null)
            EndDrag(cursorWorldPos, cursorScreenPos);
    }

    private Vector2 GetCursorScreenPos()
    {
        return mouseFollower != null
            ? mouseFollower.virtualScreenPos
            : (Vector2)Input.mousePosition;
    }

    private Vector2 GetCursorWorldPos(Vector2 cursorScreenPos)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(cursorScreenPos.x, cursorScreenPos.y, 10f));
        return (Vector2)worldPos;
    }

    private void TryBeginDrag(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(cursorWorldPos, detectionRadius, dragMask);
        if (hits.Length == 0) return;

        Collider2D topCollider = FindTopmostCollider(hits);

        selectedDragable = topCollider.GetComponent<IDragable>();
        if (selectedDragable == null) return;

        selectedRb = selectedDragable.GetRigidbody();
        dragOffset = (Vector2)selectedDragable.GetTransform().position - cursorWorldPos;
        lastCursorScreenPos = cursorScreenPos;
        cursorVelocity = Vector2.zero;
        selectedDragable.OnStartDrag();
    }

    private void UpdateDrag(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        cursorVelocity = (cursorScreenPos - lastCursorScreenPos) / Time.deltaTime;
        lastCursorScreenPos = cursorScreenPos;

        Vector2 targetPos = cursorWorldPos + dragOffset;
        selectedRb.MovePosition(targetPos);
        selectedDragable.OnDrag(targetPos);
    }

    private void EndDrag(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        float pixelsPerUnit = Screen.height / (mainCamera.orthographicSize * 2f);
        Vector2 worldVelocity = cursorVelocity / pixelsPerUnit;

        Vector2 impulse = worldVelocity * throwSensitivity;

        float clampedMagnitude = Mathf.Clamp(impulse.magnitude, minThrowForce, maxThrowForce);
        impulse = impulse.magnitude > 0f ? impulse.normalized * clampedMagnitude : Vector2.zero;

        float screenDelta = (cursorScreenPos - lastCursorScreenPos).magnitude;
        if (screenDelta >= minThrowDistance || cursorVelocity.magnitude > 0f)
            selectedDragable.OnEndDrag(impulse);
        else
            selectedDragable.OnEndDrag(Vector2.zero);

        if (selectedRb != null)
            selectedRb.linearVelocity = impulse;

        selectedDragable = null;
        selectedRb = null;
        cursorVelocity = Vector2.zero;
    }

    private static Collider2D FindTopmostCollider(Collider2D[] hits)
    {
        Collider2D topCollider = hits[0];
        for (int i = 1; i < hits.Length; i++)
        {
            if (hits[i].transform.position.z > topCollider.transform.position.z)
                topCollider = hits[i];
        }

        return topCollider;
    }
}