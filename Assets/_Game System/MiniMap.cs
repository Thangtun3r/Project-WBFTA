using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage))]
public class ViewportMapRenderer : MonoBehaviour
{
    [System.Serializable]
    public struct MapMarkerTag
    {
        public string tagName;
        public Color32 mapColor;
    }

    [Header("References")]
    public GridManager grid;
    public Camera mainCamera;

    [Header("Settings")]
    public List<MapMarkerTag> markerTags;
    public Color32 fogColor = new Color32(0, 0, 0, 255);
    public Color32 revealedColor = new Color32(0, 0, 0, 0);

    private RawImage _display;
    private Texture2D _fogTex;
    private Color32[] _pixels;
    private bool[] _isDiscovered; // Persistent record of what the player has seen

    private Vector3 _lastCamPos;
    private float _lastCamSize;

    void Start()
    {
        _display = GetComponent<RawImage>();
        if (mainCamera == null) mainCamera = Camera.main;

        MatchAspectRatio();

        // Initialize discovery array and texture
        _isDiscovered = new bool[grid.width * grid.height];
        _fogTex = new Texture2D(grid.width, grid.height);
        _fogTex.filterMode = FilterMode.Point;
        _fogTex.wrapMode = TextureWrapMode.Clamp;
        
        _pixels = new Color32[grid.width * grid.height];
        
        // Start fully hidden
        ResetMap();
        
        _display.texture = _fogTex;
    }

    void Update()
    {
        // We update every frame or on a timer because tagged objects (enemies) move
        // and need to be redrawn even if the camera is still.
        UpdateMap();
    }

    public void ResetMap()
    {
        for (int i = 0; i < _pixels.Length; i++)
        {
            _pixels[i] = fogColor;
            _isDiscovered[i] = false;
        }
    }

    void UpdateMap()
    {
        // 1. Reveal new areas based on camera viewport
        ScanViewport();

        // 2. Clear the dynamic pixel data back to the "Discovered" state
        // This prevents moving objects from leaving trails.
        for (int i = 0; i < _pixels.Length; i++)
        {
            _pixels[i] = _isDiscovered[i] ? revealedColor : fogColor;
        }

        // 3. Draw Tagged Markers
        DrawMarkers();

        // 4. Finalize Texture
        _fogTex.SetPixels32(_pixels);
        _fogTex.Apply();
    }

    void ScanViewport()
    {
        Vector3 bl = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 tr = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

        Vector2Int gridBL = grid.GetGridPosition(bl);
        Vector2Int gridTR = grid.GetGridPosition(tr);

        for (int x = gridBL.x; x <= gridTR.x; x++)
        {
            for (int y = gridBL.y; y <= gridTR.y; y++)
            {
                if (x >= 0 && x < grid.width && y >= 0 && y < grid.height)
                {
                    _isDiscovered[y * grid.width + x] = true;
                }
            }
        }
    }

    void DrawMarkers()
    {
        foreach (var marker in markerTags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(marker.tagName);
            
            foreach (GameObject obj in objects)
            {
                Vector2Int pos = grid.GetGridPosition(obj.transform.position);

                // Only draw if within grid bounds
                if (pos.x >= 0 && pos.x < grid.width && pos.y >= 0 && pos.y < grid.height)
                {
                    int index = pos.y * grid.width + pos.x;

                    // CHECK: Only show the marker if that specific tile is discovered
                    if (_isDiscovered[index])
                    {
                        _pixels[index] = marker.mapColor;
                    }
                }
            }
        }
    }

    void MatchAspectRatio()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float ratio = (float)grid.width / (float)grid.height;
        float currentWidth = rt.rect.width;
        rt.sizeDelta = new Vector2(currentWidth, currentWidth / ratio);
    }
}