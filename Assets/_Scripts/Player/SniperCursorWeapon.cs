using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
[ExecuteAlways]
public class SniperCursorWeapon : MonoBehaviour, IPlayerWeapon, IPlayerWeaponIconRotation
{
    private const string PreviewRootName = "__SniperCursorWeaponPreview";
    [Header("Prefabs")]
    [SerializeField] private GameObject pointAPrefab;
    [SerializeField] private GameObject pointBPrefab;
    [SerializeField] private GameObject stretchPrefab;
    [SerializeField] private GameObject collapsingSpriteGroupPrefab;
    [Header("Settings")]
    [SerializeField] private int mouseButton = 0;
    [SerializeField] private float minDragDistance = 0.05f;
    [SerializeField] private float maxPullDistance = 4f;
    [SerializeField] private float stretchRotationOffset;
    [SerializeField] private float pointBRotationOffset;
    [SerializeField] private float collapseDuration = 0.12f;
    [SerializeField] private bool clearOnRelease = true;
    [SerializeField] private TMP_Text chargeText;
    [Header("Weapon Cohort")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float procCoefficient = 1f;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private float iconRotationOffset;
    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditor;
    [SerializeField] private Vector3 previewPointA;
    [SerializeField] private Vector3 previewPointB = Vector3.right * 3f;
    private MouseFollower _mouseFollower;
    private Camera _camera;
    private Transform _previewRoot;
    private GameObject _pointA, _pointB, _stretch, _collapsingGroup;
    private SniperCursorWeaponProjectile _stretchProjectile;
    private StretchLayer _stretchLayer;
    private readonly List<StretchLayer> _collapsingLayers = new List<StretchLayer>();
    private Vector3 _pointAPosition;
    private Vector2 _releaseDirection;
    private bool _active = true;
    private bool _isDragging;
    private Coroutine _collapseRoutine;
    public Vector2 PullVector { get; private set; }
    public Vector2 ReleaseDirection => PullVector.sqrMagnitude > 0f ? -PullVector.normalized : Vector2.zero;
    public float DamageMultiplier => damageMultiplier;
    public float ProcCoefficient => procCoefficient;
    public Sprite CurrentSprite => iconSprite;
    public bool TryGetIconRotation(out Quaternion rotation)
    {
        Vector2 directionToPointA = -PullVector;
        if (directionToPointA.sqrMagnitude <= 0.0001f)
        {
            rotation = Quaternion.identity;
            return false;
        }

        float angle = Mathf.Atan2(directionToPointA.y, directionToPointA.x) * Mathf.Rad2Deg + iconRotationOffset;
        rotation = Quaternion.Euler(0f, 0f, angle);
        return true;
    }
    private struct StretchLayer
    {
        public SpriteRenderer Renderer;
        public Vector2 StartSize;
    }
    private void Awake()
    {
        _camera = Camera.main;
        _mouseFollower = FindFirstObjectByType<MouseFollower>();
    }
    private void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateEditorPreview();
            return;
        }
        if (!_active) return;

        Vector3 cursor = GetCursorWorldPosition();
        if (Input.GetMouseButtonDown(mouseButton)) StartPull(cursor);
        if (_isDragging && Input.GetMouseButton(mouseButton)) UpdatePull(cursor);
        if (_isDragging && Input.GetMouseButtonUp(mouseButton)) StopPull();
    }
    public void SetWeaponActive(bool active)
    {
        if (_active == active) return;

        _active = active;
        if (_active) return;

        if (_collapseRoutine != null)
        {
            StopCoroutine(_collapseRoutine);
            _collapseRoutine = null;
        }

        _isDragging = false;
        PullVector = Vector2.zero;
        ClearVisuals();
    }
    private void StartPull(Vector3 cursorPosition)
    {
        if (_collapseRoutine != null) StopCoroutine(_collapseRoutine);
        ClearVisuals();
        _isDragging = true;
        _pointAPosition = cursorPosition;
        PullVector = Vector2.zero;
        UpdateChargeText(0f);
        if (pointAPrefab != null)
            _pointA = Spawn(pointAPrefab, _pointAPosition, transform);
    }
    private void UpdatePull(Vector3 cursorPosition)
    {
        Vector3 pull = cursorPosition - _pointAPosition;
        pull.z = 0f;
        if (pull.magnitude < minDragDistance) return;
        if (pull.magnitude > maxPullDistance) pull = pull.normalized * maxPullDistance;
        Vector3 pointB = _pointAPosition + pull;
        PullVector = pull;
        _releaseDirection = ReleaseDirection;
        EnsureRuntimeVisuals(pointB);
        _pointB.transform.position = pointB;
        FacePointA(pull);
        StretchAll(_pointAPosition, pointB);
        UpdateChargeText(GetCurrentCharge01());
    }
    private void StopPull()
    {
        _isDragging = false;
        if (_pointB == null)
        {
            if (clearOnRelease) ClearVisuals();
            return;
        }
        if (_stretchProjectile != null)
            _stretchProjectile.AttachTo(_pointB.transform);
        else if (_stretch != null)
        {
            _stretch.transform.SetParent(_pointB.transform, true);
        }

        _collapseRoutine = StartCoroutine(CollapsePointB());
    }
    private IEnumerator CollapsePointB()
    {
        Vector3 start = _pointB.transform.position;
        float duration = Mathf.Max(0f, collapseDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = duration <= 0f ? 1f : elapsed / duration;
            Vector3 pointB = Vector3.Lerp(start, _pointAPosition, t);
            _pointB.transform.position = pointB;
            StretchCollapsingGroup(_pointAPosition, pointB);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _pointB.transform.position = _pointAPosition;
        StretchCollapsingGroup(_pointAPosition, _pointAPosition);
        _collapseRoutine = null;
        GameObject launchedStretch = null;
        if (_stretchProjectile != null)
        {
            launchedStretch = _stretch;
            _stretchProjectile.Launch(_releaseDirection);
            _stretch = null;
            _stretchProjectile = null;
        }
        PullVector = Vector2.zero;
        UpdateChargeText(0f);
        if (clearOnRelease) ClearVisuals(launchedStretch);
    }
    private void EnsureRuntimeVisuals(Vector3 pointB)
    {
        if (_pointB == null && pointBPrefab != null)
            _pointB = Spawn(pointBPrefab, pointB, transform);
        if (_stretch == null && stretchPrefab != null)
            CreateStretch(_pointAPosition, transform);
        if (_collapsingGroup == null && collapsingSpriteGroupPrefab != null)
            CreateCollapsingGroup(transform);
    }
    private void UpdateEditorPreview()
    {
        if (!previewInEditor)
        {
            ClearVisuals();
            return;
        }
        Transform parent = GetPreviewRoot();
        Vector3 pointA = transform.TransformPoint(previewPointA);
        Vector3 pointB = transform.TransformPoint(previewPointB);
        Vector3 pull = pointB - pointA;
        pull.z = 0f;
        if (_pointA == null && pointAPrefab != null)
            _pointA = Spawn(pointAPrefab, pointA, parent);
        if (_pointB == null && pointBPrefab != null)
            _pointB = Spawn(pointBPrefab, pointB, parent);
        if (_stretch == null && stretchPrefab != null) CreateStretch(pointA, parent);
        if (_collapsingGroup == null && collapsingSpriteGroupPrefab != null) CreateCollapsingGroup(parent);
        if (_pointA != null) _pointA.transform.position = pointA;
        if (_pointB != null)
        {
            _pointB.transform.position = pointB;
            FacePointA(pull);
        }
        StretchAll(pointA, pointB);
        UpdateChargeText(GetCurrentCharge01());
    }
    private void StretchAll(Vector3 pointA, Vector3 pointB)
    {
        StretchPersistent(pointA, pointB);
        StretchCollapsingGroup(pointA, pointB);
    }
    private void CreateStretch(Vector3 position, Transform parent)
    {
        _stretch = Spawn(stretchPrefab, position, parent);
        _stretchProjectile = _stretch.GetComponent<SniperCursorWeaponProjectile>();
        if (_stretchProjectile == null)
            _stretchLayer = CreateLayer(_stretch.GetComponentInChildren<SpriteRenderer>());
    }
    private void CreateCollapsingGroup(Transform parent)
    {
        _collapsingGroup = Spawn(collapsingSpriteGroupPrefab, transform.position, parent);
        _collapsingLayers.Clear();
        SpriteRenderer[] renderers = _collapsingGroup.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
            _collapsingLayers.Add(CreateLayer(renderers[i]));
    }
    private static StretchLayer CreateLayer(SpriteRenderer renderer)
    {
        if (renderer == null) return default;
        renderer.drawMode = SpriteDrawMode.Tiled;
        return new StretchLayer { Renderer = renderer, StartSize = renderer.size };
    }
    private void StretchPersistent(Vector3 pointA, Vector3 pointB)
    {
        if (_stretch == null) return;
        if (_stretchProjectile != null)
        {
            _stretchProjectile.StretchBetween(pointA, pointB, stretchRotationOffset);
            return;
        }
        AlignStretch(_stretch.transform, pointA, pointB);
        ResizeLayer(_stretchLayer, pointA, pointB);
    }
    private void StretchCollapsingGroup(Vector3 pointA, Vector3 pointB)
    {
        if (_collapsingGroup == null) return;
        AlignStretch(_collapsingGroup.transform, pointA, pointB);
        for (int i = 0; i < _collapsingLayers.Count; i++)
            ResizeLayer(_collapsingLayers[i], pointA, pointB);
    }
    private void AlignStretch(Transform target, Vector3 pointA, Vector3 pointB)
    {
        Vector3 direction = pointB - pointA;
        direction.z = 0f;
        target.position = pointA;
        target.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + stretchRotationOffset);
    }
    private static void ResizeLayer(StretchLayer layer, Vector3 pointA, Vector3 pointB)
    {
        if (layer.Renderer == null) return;
        float distance = Vector2.Distance(pointA, pointB);
        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(layer.Renderer.transform.lossyScale.x));
        layer.Renderer.size = new Vector2(distance / scaleX, layer.StartSize.y);
    }
    private float GetCurrentCharge01()
    {
        float maxDistance = Mathf.Max(0.0001f, maxPullDistance);
        float distance = _stretchProjectile != null ? _stretchProjectile.CurrentDistance : PullVector.magnitude;
        return Mathf.Clamp01(distance / maxDistance);
    }
    private void UpdateChargeText(float charge01)
    {
        if (chargeText == null) return;
        chargeText.text = Mathf.RoundToInt(charge01 * 100f) + "%";
    }
    private void FacePointA(Vector3 directionFromA)
    {
        if (_pointB == null) return;
        Vector3 directionToA = -directionFromA;
        float angle = Mathf.Atan2(directionToA.y, directionToA.x) * Mathf.Rad2Deg + pointBRotationOffset;
        _pointB.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    private Vector3 GetCursorWorldPosition()
    {
        if (_camera == null) _camera = Camera.main;
        Vector3 position = _mouseFollower != null
            ? _mouseFollower.GetWorldCursorPositionForFrame(_camera)
            : _camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
        position.z = transform.position.z;
        return position;
    }
    private Transform GetPreviewRoot()
    {
        if (_previewRoot != null) return _previewRoot;
        Transform found = transform.Find(PreviewRootName);
        if (found != null) return _previewRoot = found;
        GameObject root = new GameObject(PreviewRootName) { hideFlags = HideFlags.DontSaveInEditor };
        root.transform.SetParent(transform, false);
        return _previewRoot = root.transform;
    }
    private static GameObject Spawn(GameObject prefab, Vector3 position, Transform parent)
    {
        GameObject instance = Instantiate(prefab, position, Quaternion.identity, parent);
        instance.name = prefab.name;
        return instance;
    }
    private void ClearVisuals(GameObject preserve = null)
    {
        DestroyVisual(_pointA);
        DestroyVisual(_pointB);
        if (_stretch != preserve) DestroyVisual(_stretch);
        DestroyVisual(_collapsingGroup);
        _pointA = null;
        _pointB = null;
        if (_stretch != preserve) _stretch = null;
        _collapsingGroup = null;
        if (_stretch != preserve) _stretchProjectile = null;
        _stretchLayer = default;
        _collapsingLayers.Clear();
        UpdateChargeText(0f);
        if (!Application.isPlaying)
        {
            DestroyVisual(_previewRoot != null ? _previewRoot.gameObject : null);
            _previewRoot = null;
        }
    }
    private static void DestroyVisual(GameObject target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
