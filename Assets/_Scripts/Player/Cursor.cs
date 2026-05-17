using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class MouseFollower : MonoBehaviour
{
    public GameObject cursor;

    [Header("Sensitivity Settings")]
    public float sensitivity = 2.0f;

    [Header("UI Raycast Tuning")]
    [SerializeField] private Vector2 uiRaycastOffset;
    [SerializeField] private float uiRaycastRadius = 0f;

    [Header("Debug")]
    [SerializeField] private bool drawVirtualPointerGizmo = true;
    [SerializeField] private float gizmoRadius = 0.15f;
    [SerializeField] private Color gizmoColor = Color.cyan;

    public Vector2 virtualScreenPos { get; private set; }
    public bool IsHoveringUI => _lastHoveredUI != null;

    // Track last hovered UI object to send exit events
    private GameObject _lastHoveredUI;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        virtualScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    private void LateUpdate()
    {
        MoveCursor();
        UpdateUIHover();
    }

    private void MoveCursor()
    {
        float dx = Input.GetAxisRaw("Mouse X") * sensitivity;
        float dy = Input.GetAxisRaw("Mouse Y") * sensitivity;

        virtualScreenPos = new Vector2(
            Mathf.Clamp(virtualScreenPos.x + dx, 0, Screen.width),
            Mathf.Clamp(virtualScreenPos.y + dy, 0, Screen.height)
        );

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(
            new Vector3(virtualScreenPos.x, virtualScreenPos.y, 10f));
        worldPoint.z = cursor.transform.position.z;
        cursor.transform.position = worldPoint;
    }

    private void UpdateUIHover()
    {
        if (EventSystem.current == null) return;

        Vector2 raycastScreenPos = GetUIRaycastScreenPos();
        RaycastResult hitResult;
        GameObject hitUI = GetFirstInteractiveUIAtPosition(raycastScreenPos, out hitResult);

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = raycastScreenPos
        };

        if (_lastHoveredUI != null && _lastHoveredUI != hitUI)
        {
            ExecuteEvents.Execute(_lastHoveredUI, pointerData, ExecuteEvents.pointerExitHandler);
        }

        if (hitUI != null && hitUI != _lastHoveredUI)
        {
            ExecuteEvents.Execute(hitUI, pointerData, ExecuteEvents.pointerEnterHandler);
        }

        _lastHoveredUI = hitUI;

        if (hitUI == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            pointerData.pressPosition = raycastScreenPos;
            pointerData.pointerPressRaycast = hitResult;
            pointerData.pointerPress = hitUI;
            ExecuteEvents.Execute(hitUI, pointerData, ExecuteEvents.pointerDownHandler);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ExecuteEvents.Execute(hitUI, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hitUI, pointerData, ExecuteEvents.pointerClickHandler);
            pointerData.pointerPress = null;
        }
    }

    public Vector2 GetUIRaycastScreenPos()
    {
        return virtualScreenPos + uiRaycastOffset;
    }

    private void OnDrawGizmos()
    {
        if (!drawVirtualPointerGizmo)
            return;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        Vector2 screenPos = Application.isPlaying
            ? GetUIRaycastScreenPos()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));

        if (cursor != null)
            worldPoint.z = cursor.transform.position.z;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(worldPoint, gizmoRadius);
        Gizmos.DrawLine(worldPoint + Vector3.left * gizmoRadius, worldPoint + Vector3.right * gizmoRadius);
        Gizmos.DrawLine(worldPoint + Vector3.up * gizmoRadius, worldPoint + Vector3.down * gizmoRadius);
    }

    private GameObject GetFirstInteractiveUIAtPosition(Vector2 screenPos, out RaycastResult hitResult)
    {
        GameObject hitUI = RaycastInteractiveUI(screenPos, out hitResult);
        if (hitUI != null || uiRaycastRadius <= 0f)
            return hitUI;

        Vector2[] offsets =
        {
            new Vector2(uiRaycastRadius, 0f),
            new Vector2(-uiRaycastRadius, 0f),
            new Vector2(0f, uiRaycastRadius),
            new Vector2(0f, -uiRaycastRadius),
            new Vector2(uiRaycastRadius, uiRaycastRadius),
            new Vector2(-uiRaycastRadius, uiRaycastRadius),
            new Vector2(uiRaycastRadius, -uiRaycastRadius),
            new Vector2(-uiRaycastRadius, -uiRaycastRadius)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            hitUI = RaycastInteractiveUI(screenPos + offsets[i], out hitResult);
            if (hitUI != null)
                return hitUI;
        }

        hitResult = default;
        return null;
    }

    private static GameObject RaycastInteractiveUI(Vector2 screenPos, out RaycastResult hitResult)
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return GetFirstInteractiveUI(results, out hitResult);
    }

    private static GameObject GetFirstInteractiveUI(List<RaycastResult> results, out RaycastResult hitResult)
    {
        for (int i = 0; i < results.Count; i++)
        {
            GameObject candidate = results[i].gameObject;
            if (candidate == null)
                continue;

            GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(candidate);
            if (clickHandler != null)
            {
                hitResult = results[i];
                return clickHandler;
            }

            GameObject downHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(candidate);
            if (downHandler != null)
            {
                hitResult = results[i];
                return downHandler;
            }

            GameObject enterHandler = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(candidate);
            if (enterHandler != null)
            {
                hitResult = results[i];
                return enterHandler;
            }

            Selectable selectable = candidate.GetComponentInParent<Selectable>();
            if (selectable != null)
            {
                hitResult = results[i];
                return selectable.gameObject;
            }
        }

        hitResult = default;
        return null;
    }
}
