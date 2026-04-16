using UnityEngine;
using System.Collections.Generic;

public class WizardDesignerPen : MonoBehaviour
{
    [Header("Tracking Settings")]
    public Transform playerTransform; // Assign your Player GameObject here
    public bool usePlayerInsteadOfMouse = true;

    [Header("Curve Settings")]
    public int maxAnchors = 8;
    public float handleSensitivity = 1.0f;
    public int resolution = 30;
    public bool IsCurrentlyDrawing => isDragging;

    [Header("Visual Styles")]
    [Range(0.01f, 0.2f)] public float mainWidth = 0.05f;
    [Range(0.01f, 0.2f)] public float previewWidth = 0.03f;
    [Range(0.005f, 0.1f)] public float stemWidth = 0.015f;
    
    public Color previewColor = new Color(0, 0.6f, 1f, 0.4f);
    public Color finalCurveColor = Color.white;

    [Header("Prefabs")]
    public GameObject anchorPrefab;
    public GameObject handleIconPrefab;
    public GameObject stemLinePrefab;

    private LineRenderer mainLine;
    private LineRenderer previewLine; 
    private List<AnchorData> path = new List<AnchorData>();
    private bool isDragging = false;

    private class AnchorData {
        public Vector3 pos;
        public Vector3 handleForward;
        public Vector3 handleBackward;
        public GameObject anchorObj, handleFObj, handleBObj;
        public LineRenderer stemF, stemB;
    }

    void Start()
    {
        mainLine = GetComponent<LineRenderer>();
        mainLine.startWidth = mainLine.endWidth = mainWidth;
        mainLine.startColor = mainLine.endColor = finalCurveColor;

        GameObject pObj = new GameObject("RubberBand_Preview");
        pObj.transform.SetParent(transform);
        previewLine = pObj.AddComponent<LineRenderer>();
        previewLine.material = mainLine.material;
        previewLine.startColor = previewLine.endColor = previewColor;
        previewLine.positionCount = 0;
    }

    void Update()
    {
        // Get position from Player or Mouse
        Vector3 currentPos = GetCurrentInputPos();
        
        mainLine.startWidth = mainLine.endWidth = mainWidth;
        previewLine.startWidth = previewLine.endWidth = previewWidth;

        // 1. START DRAWING (Click while player is at a location)
        if (Input.GetMouseButtonDown(0) && path.Count < maxAnchors)
        {
            if (path.Count > 0) SetUIVisibility(path.Count - 1, false);

            AddNewAnchor(currentPos);
            isDragging = true;
            previewLine.positionCount = 0; 
        }

        // 2. WHILE DRAWING (Drag handle based on player movement)
        if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateActiveHandle(currentPos);
            RenderMainCurve();
        }

        // 3. STOP DRAWING
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (path.Count >= maxAnchors) SetUIVisibility(path.Count - 1, false);
        }

        // 4. PREVIEW MODE
        if (!isDragging && path.Count > 0 && path.Count < maxAnchors)
        {
            RenderRubberBand(currentPos);
        }
        else
        {
            previewLine.positionCount = 0; 
        }

        if (Input.GetMouseButtonDown(1)) ResetTool();
    }

    // Logic to decide between Player Position or Mouse
    Vector3 GetCurrentInputPos()
    {
        if (usePlayerInsteadOfMouse && playerTransform != null)
        {
            Vector3 pPos = playerTransform.position;
            pPos.z = 0f; // Force Stay 2D
            return pPos;
        }
        
        // Fallback to Mouse
        Vector3 mPos = Input.mousePosition;
        mPos.z = 10f; 
        Vector3 worldM = Camera.main.ScreenToWorldPoint(mPos);
        worldM.z = 0f; // Force Stay 2D
        return worldM;
    }

    // --- REMAINDER OF YOUR EXISTING METHODS ---
    void RenderRubberBand(Vector3 targetPos)
    {
        previewLine.positionCount = resolution;
        AnchorData last = path[path.Count - 1];
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            Vector3 p = CalculateCubic(t, last.pos, last.handleForward, targetPos, targetPos);
            previewLine.SetPosition(i, p);
        }
    }

    void AddNewAnchor(Vector3 pos)
    {
        AnchorData data = new AnchorData {
            pos = pos, handleForward = pos, handleBackward = pos,
            anchorObj = Instantiate(anchorPrefab, pos, Quaternion.identity, transform),
            handleFObj = Instantiate(handleIconPrefab, pos, Quaternion.identity, transform),
            handleBObj = Instantiate(handleIconPrefab, pos, Quaternion.identity, transform),
            stemF = Instantiate(stemLinePrefab, transform).GetComponent<LineRenderer>(),
            stemB = Instantiate(stemLinePrefab, transform).GetComponent<LineRenderer>()
        };
        data.stemF.startWidth = data.stemF.endWidth = stemWidth;
        data.stemB.startWidth = data.stemB.endWidth = stemWidth;
        path.Add(data);
    }

    void UpdateActiveHandle(Vector3 targetPos)
    {
        AnchorData active = path[path.Count - 1];
        Vector3 offset = (targetPos - active.pos) * handleSensitivity;
        active.handleForward = active.pos + offset;
        active.handleBackward = active.pos - offset;
        active.handleFObj.transform.position = active.handleForward;
        active.handleBObj.transform.position = active.handleBackward;
        active.stemF.SetPosition(0, active.pos);
        active.stemF.SetPosition(1, active.handleForward);
        active.stemB.SetPosition(0, active.pos);
        active.stemB.SetPosition(1, active.handleBackward);
    }

    void RenderMainCurve()
    {
        if (path.Count < 2) return;
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < path.Count - 1; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)(resolution - 1);
                points.Add(CalculateCubic(t, path[i].pos, path[i].handleForward, path[i+1].handleBackward, path[i+1].pos));
            }
        }
        mainLine.positionCount = points.Count;
        mainLine.SetPositions(points.ToArray());
    }

    void SetUIVisibility(int index, bool visible)
    {
        if (index < 0 || index >= path.Count) return;
        AnchorData d = path[index];
        if(d.handleFObj) d.handleFObj.SetActive(visible); 
        if(d.handleBObj) d.handleBObj.SetActive(visible);
        if(d.stemF) d.stemF.gameObject.SetActive(visible); 
        if(d.stemB) d.stemB.gameObject.SetActive(visible);
    }

    Vector3 CalculateCubic(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        return u*u*u*p0 + 3*u*u*t*p1 + 3*u*t*t*p2 + t*t*t*p3;
    }

    void ResetTool()
    {
        foreach (var d in path) {
            Destroy(d.anchorObj); Destroy(d.handleFObj); Destroy(d.handleBObj);
            Destroy(d.stemF.gameObject); Destroy(d.stemB.gameObject);
        }
        path.Clear();
        mainLine.positionCount = 0;
        previewLine.positionCount = 0;
    }
}