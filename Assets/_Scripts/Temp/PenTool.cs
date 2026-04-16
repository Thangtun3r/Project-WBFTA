using UnityEngine;
using System.Collections.Generic;

public class WizardVectorPen : MonoBehaviour
{
    [Header("Settings")]
    public int maxPoints = 6;
    public float handleSensitivity = 1.5f; // Multiplier for drag distance
    public int curveResolution = 20;

    [Header("Prefabs")]
    public GameObject dotPrefab;        // The "Anchor" visual
    public GameObject handleLinePrefab; // The line connecting Anchor to Handle

    private LineRenderer mainLine;
    private List<Vector3> anchors = new List<Vector3>();
    private List<Vector3> handles = new List<Vector3>();
    
    // Visual Object Tracking
    private List<GameObject> dotPool = new List<GameObject>();
    private List<LineRenderer> handlePool = new List<LineRenderer>();

    private bool isDragging = false;
    private Vector3 dragStartMousePos;
    private Vector3 initialHandlePos;

    void Start()
    {
        mainLine = GetComponent<LineRenderer>();
    }

    void Update()
    {
        Vector3 mousePos = GetMouseWorldPos();

        // 1. CLICK TO ADD POINT
        if (Input.GetMouseButtonDown(0) && anchors.Count < maxPoints)
        {
            anchors.Add(mousePos);
            handles.Add(mousePos); // Handle starts at anchor point
            
            CreateVisuals(mousePos);
            
            dragStartMousePos = mousePos;
            initialHandlePos = mousePos;
            isDragging = true;
        }

        // 2. DRAG TO BEND (SENSITIVITY)
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mouseDelta = mousePos - dragStartMousePos;
            // Apply sensitivity to the drag distance
            handles[handles.Count - 1] = initialHandlePos + (mouseDelta * handleSensitivity);
            
            UpdateVisuals();
            RenderSpline();
        }

        if (Input.GetMouseButtonUp(0)) isDragging = false;

        // RESET
        if (Input.GetMouseButtonDown(1)) ResetPen();
    }

    void CreateVisuals(Vector3 pos)
    {
        // Create the Dot
        GameObject dot = Instantiate(dotPrefab, pos, Quaternion.identity, transform);
        dotPool.Add(dot);

        // Create the Handle Line
        GameObject hLineObj = Instantiate(handleLinePrefab, transform);
        LineRenderer hLine = hLineObj.GetComponent<LineRenderer>();
        handlePool.Add(hLine);
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < anchors.Count; i++)
        {
            // Move dots to anchor positions
            dotPool[i].transform.position = anchors[i];

            // Update handle lines (Connect anchor to handle)
            handlePool[i].positionCount = 2;
            handlePool[i].SetPosition(0, anchors[i]);
            handlePool[i].SetPosition(1, handles[i]);
        }
    }

    void RenderSpline()
    {
        if (anchors.Count < 2) return;

        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < anchors.Count - 1; i++)
        {
            for (int j = 0; j < curveResolution; j++)
            {
                float t = j / (float)(curveResolution - 1);
                // Quadratic Bezier: P0=Anchor[i], P1=Handle[i+1], P2=Anchor[i+1]
                Vector3 p = CalculateBezier(t, anchors[i], handles[i+1], anchors[i+1]);
                points.Add(p);
            }
        }
        mainLine.positionCount = points.Count;
        mainLine.SetPositions(points.ToArray());
    }

    Vector3 CalculateBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }

    void ResetPen()
    {
        anchors.Clear(); handles.Clear();
        foreach (var d in dotPool) Destroy(d);
        foreach (var h in handlePool) Destroy(h.gameObject);
        dotPool.Clear(); handlePool.Clear();
        mainLine.positionCount = 0;
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 p = Input.mousePosition; p.z = 10f;
        return Camera.main.ScreenToWorldPoint(p);
    }
}