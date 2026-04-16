using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ViewportMapRenderer : MonoBehaviour
{
    public GridManager grid;
    public Camera mainCamera;
    
    private RawImage _display;
    private Texture2D _fogTex;
    private Color32[] _pixels;
    
    // We'll use this to prevent redundant texture updates
    private Vector3 _lastCamPos;
    private float _lastCamSize;

    void Start()
    {
        _display = GetComponent<RawImage>();
        if (mainCamera == null) mainCamera = Camera.main;

        // 1. Auto-Scale the UI Rect to match Grid Ratio
        MatchAspectRatio();

        // 2. Setup Texture
        _fogTex = new Texture2D(grid.width, grid.height);
        _fogTex.filterMode = FilterMode.Point;
        _fogTex.wrapMode = TextureWrapMode.Clamp;
        
        _pixels = new Color32[grid.width * grid.height];
        for (int i = 0; i < _pixels.Length; i++) _pixels[i] = new Color32(0, 0, 0, 255);

        _fogTex.SetPixels32(_pixels);
        _fogTex.Apply();
        _display.texture = _fogTex;
    }

    void Update()
    {
        // Only update if the camera moves or changes zoom (orthographicSize)
        if (mainCamera.transform.position != _lastCamPos || mainCamera.orthographicSize != _lastCamSize)
        {
            ScanViewport();
            _lastCamPos = mainCamera.transform.position;
            _lastCamSize = mainCamera.orthographicSize;
        }
    }

    void MatchAspectRatio()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float ratio = (float)grid.width / (float)grid.height;
        
        // Adjust height based on current width to maintain ratio
        float currentWidth = rt.rect.width;
        rt.sizeDelta = new Vector2(currentWidth, currentWidth / ratio);
    }

    void ScanViewport()
    {
        // 3. Find Viewport Bounds in World Space
        // Bottom Left
        Vector3 bl = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        // Top Right
        Vector3 tr = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

        // 4. Convert World Bounds to Grid Bounds
        Vector2Int gridBL = grid.GetGridPosition(bl);
        Vector2Int gridTR = grid.GetGridPosition(tr);

        bool changed = false;

        // 5. Fill all tiles within the viewport rectangle
        for (int x = gridBL.x; x <= gridTR.x; x++)
        {
            for (int y = gridBL.y; y <= gridTR.y; y++)
            {
                if (x >= 0 && x < grid.width && y >= 0 && y < grid.height)
                {
                    int index = y * grid.width + x;
                    if (_pixels[index].a != 0)
                    {
                        _pixels[index] = new Color32(0, 0, 0, 0);
                        changed = true;
                    }
                }
            }
        }

        if (changed)
        {
            _fogTex.SetPixels32(_pixels);
            _fogTex.Apply();
        }
    }
}