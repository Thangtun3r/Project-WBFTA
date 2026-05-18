using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private float throwInvincibilityDuration = 0.25f;

    [Header("Visual Effects")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float scaleSpeed = 0.2f;
    [SerializeField] private float damageMultiplier = 1.0f;

    private IDragable selectedDragable;
    private Rigidbody2D selectedRb;
    private Vector2 dragOffset;
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

    private void Update()
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

        selectedDragable = topCollider.GetComponent<IDragable>();
        if (selectedDragable == null) return;

        selectedRb = selectedDragable.GetRigidbody();
        
        if (selectedRb != null)
        {
            selectedRb.linearVelocity = Vector2.zero;
            selectedRb.angularVelocity = 0f;
            selectedRb.isKinematic = true;

            Collider2D col = selectedRb.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        dragOffset = (Vector2)selectedDragable.GetTransform().position - cursorWorldPos;
        lastCursorScreenPos = cursorScreenPos;
        cursorVelocity = Vector2.zero;
        selectedDragable.OnStartDrag();

        _visualTransform = selectedDragable.GetTransform();
        _originalScale = _visualTransform.localScale;
        StartScale(_originalScale * scaleMultiplier);
        
        // Mark throw ownership only; DragImpactDamage remains the single damage source.
        DragImpactDamage dragDamage = selectedRb.GetComponent<DragImpactDamage>();
        if (dragDamage != null && _playerStats != null)
        {
            float damageOutput = _playerStats.GetCalculatedAttackDamage() * damageMultiplier;
            bool isCrit = _playerStats.WasLastAttackCrit();
            dragDamage.SetPlayerThrowSource(damageOutput, isCrit);
        }
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
        {
            selectedRb.isKinematic = false;
            
            Collider2D col = selectedRb.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = false;
            
            selectedRb.linearVelocity = impulse;
        }

        if (impulse.sqrMagnitude > 0f)
        {
            _playerHealth?.GrantInvincibility(throwInvincibilityDuration);
        }

        StartScale(_originalScale);

        selectedDragable = null;
        selectedRb = null;
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
