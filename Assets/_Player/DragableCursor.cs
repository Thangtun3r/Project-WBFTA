using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class DragableCursor : MonoBehaviour
{
    [Header("Draggable Settings")]
    [SerializeField] private float throwSensitivity = 1f;
    [SerializeField] private LayerMask dragMask;
    [SerializeField] private float detectionRadius = 0.5f;
    [SerializeField] private bool rigidLockToCursor = true;

    [Header("Throw Force Limits")]
    [SerializeField] private float minThrowForce = 1f;
    [SerializeField] private float maxThrowForce = 20f;
    [SerializeField] private float minThrowDistance = 10f;
    private float throwInvincibilityDuration = 0.25f;

    [Header("Visual Effects")]
     private float scaleMultiplier = 1.1f;
     private float scaleSpeed = 0.035f;
    [SerializeField] private float damageMultiplier = 1.0f;

    private IDragable selectedDragable;
    private Rigidbody2D selectedRb;
    private RigidbodyType2D previousBodyType;
    private RigidbodyInterpolation2D previousInterpolation;
    private Collider2D[] selectedColliders;
    private bool[] previousColliderTriggerStates;
    private Vector2 dragOffset;
    private Vector2 dragTargetPosition;
    private bool hasDragTargetPosition;
    private Vector2 lastCursorScreenPos;
    private Vector2 cursorVelocity;
    private Camera mainCamera;
    private MouseFollower mouseFollower;
    private PlayerStatMachine _playerStats;
    private PlayerHealth _playerHealth;

    // Visual Effect State
    private Transform _visualTransform;
    private Vector3 _originalScale;
    private Coroutine _scaleCoroutine;

    private void Awake()
    {
        mainCamera = Camera.main;
        mouseFollower = FindFirstObjectByType<MouseFollower>();
        _playerStats = FindFirstObjectByType<PlayerStatMachine>();
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void LateUpdate()
    {
        Vector2 cursorScreenPos = GetCursorScreenPos();
        Vector2 cursorWorldPos = GetCursorWorldPos(cursorScreenPos);
        bool isHoveringUI = IsCursorOverUI(cursorScreenPos);

        if (isHoveringUI)
        {
            if (Input.GetMouseButtonUp(0) && selectedDragable != null)
                EndDrag(cursorWorldPos, cursorScreenPos);

            return;
        }

        if (Input.GetMouseButtonDown(0))
            TryBeginDrag(cursorWorldPos, cursorScreenPos);

        if (Input.GetMouseButton(0) && selectedDragable != null)
            UpdateDrag(cursorWorldPos, cursorScreenPos);

        if (Input.GetMouseButtonUp(0) && selectedDragable != null)
            EndDrag(cursorWorldPos, cursorScreenPos);
    }

    private void FixedUpdate()
    {
        if (rigidLockToCursor || selectedRb == null || !hasDragTargetPosition)
            return;

        selectedRb.MovePosition(dragTargetPosition);
    }

    private Vector2 GetCursorScreenPos()
    {
        return mouseFollower != null
            ? mouseFollower.GetVirtualScreenPosForFrame()
            : (Vector2)Input.mousePosition;
    }

    private Vector2 GetCursorWorldPos(Vector2 cursorScreenPos)
    {
        if (mouseFollower != null)
            return mouseFollower.GetWorldCursorPositionForFrame(mainCamera);

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(cursorScreenPos.x, cursorScreenPos.y, 10f));
        return (Vector2)worldPos;
    }

    private bool IsCursorOverUI(Vector2 cursorScreenPos)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = mouseFollower != null
                ? mouseFollower.GetUIRaycastScreenPos()
                : cursorScreenPos
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return HasInteractiveUI(results);
    }

    private static bool HasInteractiveUI(List<RaycastResult> results)
    {
        for (int i = 0; i < results.Count; i++)
        {
            GameObject candidate = results[i].gameObject;
            if (candidate == null)
                continue;

            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(candidate) != null)
                return true;

            if (ExecuteEvents.GetEventHandler<IPointerDownHandler>(candidate) != null)
                return true;

            if (candidate.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }

    private void TryBeginDrag(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(cursorWorldPos, detectionRadius, dragMask);
        if (hits.Length == 0) return;

        Collider2D topCollider = FindTopmostCollider(hits);

        selectedDragable = topCollider.GetComponentInParent<IDragable>();
        if (selectedDragable == null) return;

        selectedRb = selectedDragable.GetRigidbody();
        
        if (selectedRb != null)
        {
            previousBodyType = selectedRb.bodyType;
            previousInterpolation = selectedRb.interpolation;
            selectedRb.interpolation = RigidbodyInterpolation2D.None;
            selectedRb.linearVelocity = Vector2.zero;
            selectedRb.angularVelocity = 0f;
            selectedRb.bodyType = RigidbodyType2D.Kinematic;

            selectedColliders = selectedRb.GetComponentsInChildren<Collider2D>();
            previousColliderTriggerStates = new bool[selectedColliders.Length];
            for (int i = 0; i < selectedColliders.Length; i++)
            {
                previousColliderTriggerStates[i] = selectedColliders[i].isTrigger;
                selectedColliders[i].isTrigger = true;
            }
        }

        dragOffset = (Vector2)selectedDragable.GetTransform().position - cursorWorldPos;
        dragTargetPosition = cursorWorldPos + dragOffset;
        hasDragTargetPosition = true;
        SnapSelectedToDragTarget();
        lastCursorScreenPos = cursorScreenPos;
        cursorVelocity = Vector2.zero;
        selectedDragable.OnStartDrag();

        _visualTransform = selectedDragable.GetTransform();
        _originalScale = _visualTransform.localScale;
        StartScale(_originalScale * scaleMultiplier);
        
        if (selectedRb != null)
        {
            EnemyVisual ev = selectedRb.GetComponentInChildren<EnemyVisual>();
            ev?.SetDragState(true);
        }
        
        // Mark throw ownership only; DragImpactDamage remains the single damage source.
        DragImpactDamage dragDamage = selectedRb != null ? selectedRb.GetComponent<DragImpactDamage>() : null;
        if (dragDamage != null && _playerStats != null)
        {
            float damageOutput = _playerStats.GetCalculatedAttackDamage() * damageMultiplier;
            bool isCrit = _playerStats.WasLastAttackCrit();
            dragDamage.SetPlayerThrowSource(damageOutput, isCrit);
        }
    }

    private void UpdateDrag(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        float deltaTime = Mathf.Max(Time.deltaTime, Mathf.Epsilon);
        cursorVelocity = (cursorScreenPos - lastCursorScreenPos) / deltaTime;
        lastCursorScreenPos = cursorScreenPos;

        Vector2 targetPos = cursorWorldPos + dragOffset;
        dragTargetPosition = targetPos;
        hasDragTargetPosition = true;
        SnapSelectedToDragTarget();
        selectedDragable.OnDrag(dragTargetPosition);
    }

    private void EndDrag(Vector2 cursorWorldPos, Vector2 cursorScreenPos)
    {
        float pixelsPerUnit = Screen.height / (mainCamera.orthographicSize * 2f);
        Vector2 worldVelocity = cursorVelocity / pixelsPerUnit;

        Vector2 impulse = worldVelocity * throwSensitivity;

        float clampedMagnitude = Mathf.Clamp(impulse.magnitude, minThrowForce, maxThrowForce);
        impulse = impulse.magnitude > 0f ? impulse.normalized * clampedMagnitude : Vector2.zero;

        float screenDelta = (cursorScreenPos - lastCursorScreenPos).magnitude;
        Vector2 releaseVelocity = screenDelta >= minThrowDistance || cursorVelocity.magnitude > 0f
            ? impulse
            : Vector2.zero;

        if (selectedRb != null)
        {
            hasDragTargetPosition = false;
            selectedRb.position = cursorWorldPos + dragOffset;
            selectedRb.bodyType = previousBodyType;
            selectedRb.interpolation = previousInterpolation;

            RestoreColliderTriggerStates();
            
            selectedRb.linearVelocity = releaseVelocity;
        }

        selectedDragable.OnEndDrag(releaseVelocity);

        if (releaseVelocity.sqrMagnitude > 0f)
        {
            _playerHealth?.GrantInvincibility(throwInvincibilityDuration);
        }

        StartScale(_originalScale);

        if (selectedRb != null)
        {
            EnemyVisual ev = selectedRb.GetComponentInChildren<EnemyVisual>();
            ev?.SetDragState(false);
        }

        selectedDragable = null;
        selectedRb = null;
        selectedColliders = null;
        previousColliderTriggerStates = null;
        hasDragTargetPosition = false;
        cursorVelocity = Vector2.zero;
        _visualTransform = null;
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

    private void SnapSelectedToDragTarget()
    {
        Transform selectedTransform = selectedDragable?.GetTransform();
        if (selectedTransform != null)
        {
            Vector3 targetPosition = new Vector3(
                dragTargetPosition.x,
                dragTargetPosition.y,
                selectedTransform.position.z);

            selectedTransform.position = targetPosition;
        }

        if (selectedRb != null)
        {
            selectedRb.position = dragTargetPosition;
            selectedRb.linearVelocity = Vector2.zero;
            selectedRb.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();
    }

    private void RestoreColliderTriggerStates()
    {
        if (selectedColliders == null || previousColliderTriggerStates == null)
            return;

        int count = Mathf.Min(selectedColliders.Length, previousColliderTriggerStates.Length);
        for (int i = 0; i < count; i++)
        {
            if (selectedColliders[i] != null)
                selectedColliders[i].isTrigger = previousColliderTriggerStates[i];
        }
    }

    // -------------------------------------------------------------------------
    // Visual Effects Logic
    // -------------------------------------------------------------------------

    private void StartScale(Vector3 targetScale)
    {
        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        if (_visualTransform != null)
            _scaleCoroutine = StartCoroutine(ScaleEffect(_visualTransform, targetScale));
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
