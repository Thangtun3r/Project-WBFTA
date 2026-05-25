using System.Collections.Generic;
using UnityEngine;

public class SniperCursorWeaponProjectile : MonoBehaviour
{
    [SerializeField] private bool stretchFromPivot = true;
    [SerializeField] private bool resizeVisualWidth = true;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private BoxCollider2D hitbox;
    [Header("Damage")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private float speed = 12f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float procCoefficient = 1f;
    [SerializeField] private bool scaleDamageWithCharge = true;
    [SerializeField, Range(0.01f, 0.99f)] private float damageChargeThreshold = 0.5f;
    [SerializeField] private float belowThresholdDamageMultiplier = 0.1f;
    [SerializeField] private float minChargeDamageMultiplier = 0.25f;
    [SerializeField] private float maxChargeDamageMultiplier = 1f;
    [SerializeField] private bool forceHitboxTrigger = true;
    [SerializeField] private bool disableHitboxUntilLaunch = true;
    [SerializeField] private bool damageEachTargetOnce = true;
    [SerializeField] private bool destroyOnDamage;
    [Header("Screen Ricochet")]
    [SerializeField] private bool screenRicochetEnabled;
    [SerializeField] private int screenRicochetBounces = 2;
    [SerializeField] private float screenRicochetViewportPadding = 0.02f;
    [SerializeField] private float screenRicochetCooldown = 0.03f;
    [SerializeField] private bool pauseDuringRicochetBend;
    [SerializeField] private bool disableHitboxDuringRicochetBend = true;
    [SerializeField] private float ricochetIncomingShrinkDuration = 0.08f;
    [SerializeField] private float ricochetOutgoingExpandDuration = 0.08f;
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 5f;
    [Header("Point B Extension")]
    [SerializeField] private bool extendPointBGradually = true;
    [SerializeField] private float fastExtendSpeed = 25f;
    [SerializeField] private float slowExtendSpeed = 4f;
    [SerializeField, Range(0.01f, 0.99f)] private float fastExtendRatio = 0.5f;

    private SpriteRenderer[] _renderers;
    private Vector2[] _startSizes;
    private Vector3[] _startLocalPositions;
    private Vector2 _hitboxStartSize;
    private Vector2 _hitboxStartOffset;
    private bool _hitboxCached;

    private float _currentDistance;
    private float _targetDistance;
    private float _launchCharge01 = 1f;
    private bool _hasLaunched;
    private Vector2 _launchDirection;
    private Rigidbody2D _rigidbody;
    private Camera _ricochetCamera;
    private int _remainingScreenRicochets;
    private float _nextScreenRicochetTime;
    private Coroutine _ricochetBendRoutine;
    private bool _isRicochetBending;
    private readonly HashSet<IDamagable> _damagedTargets = new HashSet<IDamagable>();

    public float CurrentDistance => _currentDistance;
    public Transform PointA => pointA;
    public Transform PointB => pointB;

    private void Awake()
    {
        CacheRenderers();
        CacheHitbox();
        EnsureEndpointTransforms();
        _rigidbody = GetComponent<Rigidbody2D>();
        SetHitboxActive(!disableHitboxUntilLaunch);

        if (playerAttack == null)
        {
            playerAttack = GetComponentInParent<PlayerAttack>()
                ?? FindFirstObjectByType<PlayerAttack>();
        }
    }

    private void Update()
    {
        if (_hasLaunched)
            UpdateScreenRicochet();
    }

    private void OnDisable()
    {
        if (_ricochetBendRoutine != null)
        {
            StopCoroutine(_ricochetBendRoutine);
            _ricochetBendRoutine = null;
        }

        _isRicochetBending = false;
        if (_hasLaunched)
            SetHitboxActive(true);
    }

    public void StretchBetween(Vector3 targetPointA, Vector3 targetPointB, float rotationOffset)
    {
        CacheRenderers();
        _ = rotationOffset;

        SetEndpointLine(targetPointA, targetPointB, Application.isPlaying ? Time.deltaTime : 1f);
        RefreshFromEndpoints();
    }

    private void SetEndpointLine(Vector3 worldPointA, Vector3 worldPointB, float deltaTime)
    {
        Vector3 direction = worldPointB - worldPointA;
        direction.z = 0f;
        _targetDistance = direction.magnitude;

        float distance = extendPointBGradually
            ? GetNextExtendedDistance(_targetDistance, deltaTime)
            : _targetDistance;

        Vector3 pointBWorld = _targetDistance > 0f
            ? worldPointA + direction.normalized * distance
            : worldPointA;

        transform.rotation = Quaternion.Euler(
            0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.position = stretchFromPivot
            ? worldPointA
            : worldPointA + (pointBWorld - worldPointA) * 0.5f;

        ApplyWorldEndpoints(worldPointA, pointBWorld);
    }

    private void ApplyWorldEndpoints(Vector3 worldPointA, Vector3 worldPointB)
    {
        EnsureEndpointTransforms();
        pointA.position = worldPointA;
        pointB.position = worldPointB;
    }

    private float GetNextExtendedDistance(float targetDistance, float deltaTime)
    {
        if (targetDistance <= 0f)
            return 0f;

        float currentDistance = Mathf.Min(GetEndpointDistance(), targetDistance);
        if (currentDistance >= targetDistance)
            return targetDistance;

        float remainingTime = Mathf.Max(0f, deltaTime);
        float splitDistance = targetDistance * fastExtendRatio;

        if (currentDistance < splitDistance && remainingTime > 0f)
        {
            float fastSpeed = Mathf.Max(0f, fastExtendSpeed);
            if (fastSpeed <= 0f)
                return currentDistance;

            float nextDistance = Mathf.MoveTowards(currentDistance, splitDistance, fastSpeed * remainingTime);
            remainingTime -= (nextDistance - currentDistance) / fastSpeed;
            currentDistance = nextDistance;
        }

        if (currentDistance < targetDistance && remainingTime > 0f)
            currentDistance = Mathf.MoveTowards(
                currentDistance,
                targetDistance,
                Mathf.Max(0f, slowExtendSpeed) * remainingTime);

        return currentDistance;
    }

    private void RefreshFromEndpoints()
    {
        EnsureEndpointTransforms();

        _currentDistance = GetEndpointDistance();

        if (resizeVisualWidth)
            ApplyWidth(_currentDistance);

        UpdateHitbox(_currentDistance);
    }

    private void UpdateEndpoints(float distance)
    {
        EnsureEndpointTransforms();

        if (stretchFromPivot)
        {
            pointA.localPosition = Vector3.zero;
            pointB.localPosition = new Vector3(distance, 0f, 0f);
        }
        else
        {
            float half = distance * 0.5f;
            pointA.localPosition = new Vector3(-half, 0f, 0f);
            pointB.localPosition = new Vector3(half, 0f, 0f);
        }
    }

    private void ApplyWidth(float distance)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;

            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(_renderers[i].transform.lossyScale.x));
            float targetSizeX = distance / scaleX;
            
            // 1. Read the Sprite's real pivot mathematically (Center = 0.5, Left = 0.0)
            float pivotX = 0.5f;
            if (_renderers[i].sprite != null && _renderers[i].sprite.rect.width > 0f)
            {
                pivotX = _renderers[i].sprite.pivot.x / _renderers[i].sprite.rect.width;
            }

            // 2. Determine where we want to anchor the visual
            float anchorX = stretchFromPivot ? 0f : 0.5f;
            
            // 3. Calculate how much the size has changed since the projectile spawned
            float deltaSize = targetSizeX - _startSizes[i].x;
            
            // 4. Calculate the exact positional shift needed to perfectly counter the pivot bleed
            float shiftX = deltaSize * (pivotX - anchorX);

            // Apply size
            _renderers[i].size = new Vector2(targetSizeX, _startSizes[i].y);

            // Dynamically counter-steer the local position so it NEVER overshoots
            if (_renderers[i].transform != transform)
            {
                Vector3 newLocal = _startLocalPositions[i];
                newLocal.x += shiftX;
                _renderers[i].transform.localPosition = newLocal;
            }
        }
    }

    public void AttachTo(Transform parent)
    {
        transform.SetParent(parent, true);
    }

    public void ConfigureScreenRicochet(Camera ricochetCamera, int bounces)
    {
        _ricochetCamera = ricochetCamera != null ? ricochetCamera : Camera.main;
        _remainingScreenRicochets = Mathf.Max(0, bounces);
        screenRicochetEnabled = _remainingScreenRicochets > 0;
    }

    public void Launch(Vector2 direction)
    {
        transform.SetParent(null, true);
        RefreshFromEndpoints();
        _launchCharge01 = _targetDistance > 0f ? Mathf.Clamp01(_currentDistance / _targetDistance) : 1f;
        PrepareScreenRicochetLaunch();
        BeginDamage();

        Vector2 velocity = direction.sqrMagnitude > 0f ? direction.normalized * speed : Vector2.zero;
        _launchDirection = velocity.sqrMagnitude > 0f ? velocity.normalized : Vector2.zero;
        if (_rigidbody != null)
            _rigidbody.linearVelocity = velocity;

        Destroy(gameObject, lifetime);
    }

    public void LaunchFrom(Vector3 worldPointA, Vector3 worldPointB, Vector2 direction)
    {
        LaunchFrom(worldPointA, worldPointB, direction, GetEndpointDistance());
    }

    public void LaunchFrom(Vector3 worldPointA, Vector3 worldPointB, Vector2 direction, float launchDistance)
    {
        transform.SetParent(null, true);
        Vector3 line = worldPointB - worldPointA;
        line.z = 0f;
        float releaseDistance = line.magnitude;

        if (launchDistance <= 0f)
            launchDistance = releaseDistance;
        launchDistance = Mathf.Min(launchDistance, releaseDistance);

        _launchCharge01 = releaseDistance > 0f ? Mathf.Clamp01(launchDistance / releaseDistance) : 1f;

        Vector3 launchPointB = line.sqrMagnitude > 0f
            ? worldPointA + line.normalized * launchDistance
            : worldPointA;

        SetEndpointLineImmediate(worldPointA, launchPointB);
        RefreshFromEndpoints();
        PrepareScreenRicochetLaunch();
        BeginDamage();

        Vector2 velocity = direction.sqrMagnitude > 0f ? direction.normalized * speed : Vector2.zero;
        _launchDirection = velocity.sqrMagnitude > 0f ? velocity.normalized : Vector2.zero;
        if (_rigidbody != null)
            _rigidbody.linearVelocity = velocity;

        Destroy(gameObject, lifetime);
    }

    private void PrepareScreenRicochetLaunch()
    {
        if (!screenRicochetEnabled)
            return;

        if (_remainingScreenRicochets <= 0)
            _remainingScreenRicochets = Mathf.Max(0, screenRicochetBounces);

        screenRicochetEnabled = _remainingScreenRicochets > 0;
        if (screenRicochetEnabled && _ricochetCamera == null)
            _ricochetCamera = Camera.main;
    }

    private void UpdateScreenRicochet()
    {
        if (!screenRicochetEnabled || _remainingScreenRicochets <= 0 || _isRicochetBending)
            return;
        if (Time.time < _nextScreenRicochetTime)
            return;
        if (_ricochetCamera == null)
            _ricochetCamera = Camera.main;
        if (_ricochetCamera == null)
            return;

        Vector2 velocity = GetCurrentVelocity();
        if (velocity.sqrMagnitude <= 0.0001f)
            return;

        Vector2 incomingDirection = velocity.normalized;
        Vector3 leadingPoint = GetLeadingPoint(incomingDirection);
        Vector3 nextLeadingPoint = leadingPoint + (Vector3)(velocity * Time.deltaTime);
        Vector3 viewportPoint = _ricochetCamera.WorldToViewportPoint(nextLeadingPoint);
        float padding = Mathf.Clamp(screenRicochetViewportPadding, 0f, 0.49f);
        bool hitX = viewportPoint.x <= padding || viewportPoint.x >= 1f - padding;
        bool hitY = viewportPoint.y <= padding || viewportPoint.y >= 1f - padding;

        if (!hitX && !hitY)
            return;

        Vector2 reflected = incomingDirection;
        if (hitX) reflected.x *= -1f;
        if (hitY) reflected.y *= -1f;
        if (reflected.sqrMagnitude <= 0.0001f)
            return;

        Vector3 bouncePoint = GetPreciseViewportBouncePoint(leadingPoint, nextLeadingPoint, padding, hitX, hitY);
        OffsetProjectileLeadingPointTo(leadingPoint, bouncePoint);
        Vector2 outgoingDirection = reflected.normalized;
        Vector2 ricochetVelocity = outgoingDirection * velocity.magnitude;
        if (pauseDuringRicochetBend)
            BeginRicochetBend(bouncePoint, incomingDirection, outgoingDirection, ricochetVelocity);
        else
            SetLaunchVelocity(ricochetVelocity);

        OnScreenEffect.Instance?.ShakeHUD();
        _remainingScreenRicochets--;
        _nextScreenRicochetTime = Time.time + Mathf.Max(0f, screenRicochetCooldown);

        if (_remainingScreenRicochets <= 0)
            screenRicochetEnabled = false;
    }

    private Vector2 GetCurrentVelocity()
    {
        if (_rigidbody != null)
            return _rigidbody.linearVelocity;

        return _launchDirection.sqrMagnitude > 0f
            ? _launchDirection.normalized * speed
            : Vector2.zero;
    }

    private void SetLaunchVelocity(Vector2 velocity)
    {
        _launchDirection = velocity.sqrMagnitude > 0f ? velocity.normalized : Vector2.zero;
        if (_rigidbody != null)
            _rigidbody.linearVelocity = velocity;

        if (_launchDirection.sqrMagnitude > 0f)
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(_launchDirection.y, _launchDirection.x) * Mathf.Rad2Deg);
    }

    private Vector3 GetLeadingPoint(Vector2 direction)
    {
        EnsureEndpointTransforms();
        float pointADot = Vector2.Dot((Vector2)pointA.position, direction);
        float pointBDot = Vector2.Dot((Vector2)pointB.position, direction);
        return pointBDot >= pointADot ? pointB.position : pointA.position;
    }

    private Vector3 GetPreciseViewportBouncePoint(
        Vector3 leadingPoint,
        Vector3 nextLeadingPoint,
        float padding,
        bool hitX,
        bool hitY)
    {
        Vector3 startViewport = _ricochetCamera.WorldToViewportPoint(leadingPoint);
        Vector3 endViewport = _ricochetCamera.WorldToViewportPoint(nextLeadingPoint);
        Vector3 deltaViewport = endViewport - startViewport;
        float t = 1f;

        if (hitX && Mathf.Abs(deltaViewport.x) > 0.0001f)
        {
            float edgeX = endViewport.x <= padding ? padding : 1f - padding;
            t = Mathf.Min(t, Mathf.Clamp01((edgeX - startViewport.x) / deltaViewport.x));
        }

        if (hitY && Mathf.Abs(deltaViewport.y) > 0.0001f)
        {
            float edgeY = endViewport.y <= padding ? padding : 1f - padding;
            t = Mathf.Min(t, Mathf.Clamp01((edgeY - startViewport.y) / deltaViewport.y));
        }

        Vector3 bounceWorld = Vector3.Lerp(leadingPoint, nextLeadingPoint, t);
        Vector3 bounceViewport = _ricochetCamera.WorldToViewportPoint(bounceWorld);
        bounceViewport.x = Mathf.Clamp(bounceViewport.x, padding, 1f - padding);
        bounceViewport.y = Mathf.Clamp(bounceViewport.y, padding, 1f - padding);

        Vector3 preciseWorld = _ricochetCamera.ViewportToWorldPoint(bounceViewport);
        preciseWorld.z = leadingPoint.z;
        return preciseWorld;
    }

    private void OffsetProjectileLeadingPointTo(Vector3 currentLeadingPoint, Vector3 targetLeadingPoint)
    {
        transform.position += targetLeadingPoint - currentLeadingPoint;
    }

    private void BeginRicochetBend(
        Vector3 bendPoint,
        Vector2 incomingDirection,
        Vector2 outgoingDirection,
        Vector2 resumeVelocity)
    {
        if (_ricochetBendRoutine != null)
            StopCoroutine(_ricochetBendRoutine);

        _ricochetBendRoutine = StartCoroutine(RicochetBendRoutine(
            bendPoint,
            incomingDirection.normalized,
            outgoingDirection.normalized,
            resumeVelocity));
    }

    private System.Collections.IEnumerator RicochetBendRoutine(
        Vector3 bendPoint,
        Vector2 incomingDirection,
        Vector2 outgoingDirection,
        Vector2 resumeVelocity)
    {
        _isRicochetBending = true;
        SetLaunchVelocity(Vector2.zero);
        if (disableHitboxDuringRicochetBend)
            SetHitboxActive(false);

        float length = Mathf.Max(0.0001f, _currentDistance);
        yield return AnimateRicochetSegment(
            bendPoint - (Vector3)(incomingDirection * length),
            bendPoint,
            true,
            ricochetIncomingShrinkDuration);

        yield return AnimateRicochetSegment(
            bendPoint,
            bendPoint + (Vector3)(outgoingDirection * length),
            false,
            ricochetOutgoingExpandDuration);

        SetEndpointLineImmediate(bendPoint, bendPoint + (Vector3)(outgoingDirection * length));
        RefreshFromEndpoints();
        if (disableHitboxDuringRicochetBend)
            SetHitboxActive(true);

        SetLaunchVelocity(resumeVelocity);
        _isRicochetBending = false;
        _ricochetBendRoutine = null;
    }

    private System.Collections.IEnumerator AnimateRicochetSegment(
        Vector3 startPoint,
        Vector3 endPoint,
        bool shrinkToEnd,
        float duration)
    {
        duration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyRicochetSegmentFrame(startPoint, endPoint, shrinkToEnd, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyRicochetSegmentFrame(startPoint, endPoint, shrinkToEnd, 1f);
    }

    private void ApplyRicochetSegmentFrame(Vector3 startPoint, Vector3 endPoint, bool shrinkToEnd, float t)
    {
        Vector3 frameStart = shrinkToEnd ? Vector3.Lerp(startPoint, endPoint, t) : startPoint;
        Vector3 frameEnd = shrinkToEnd ? endPoint : Vector3.Lerp(startPoint, endPoint, t);
        SetEndpointLineImmediate(frameStart, frameEnd);
        RefreshFromEndpoints();
    }

    private void SetEndpointLineImmediate(Vector3 worldPointA, Vector3 worldPointB)
    {
        Vector3 direction = worldPointB - worldPointA;
        direction.z = 0f;
        _targetDistance = direction.magnitude;

        transform.rotation = Quaternion.Euler(
            0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.position = stretchFromPivot
            ? worldPointA
            : worldPointA + direction * 0.5f;

        ApplyWorldEndpoints(worldPointA, worldPointB);
    }

    private void CacheRenderers()
    {
        if (_renderers != null) return;
        EnsureEndpointTransforms();

        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();
        int validCount = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] == null) continue;
            if (IsEndpointRenderer(allRenderers[i])) continue;
            validCount++;
        }

        _renderers = new SpriteRenderer[validCount];
        _startSizes = new Vector2[_renderers.Length];
        _startLocalPositions = new Vector3[_renderers.Length];

        int writeIndex = 0;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] == null) continue;
            if (IsEndpointRenderer(allRenderers[i])) continue;
            _renderers[writeIndex++] = allRenderers[i];
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].drawMode = SpriteDrawMode.Tiled;
            _startSizes[i] = _renderers[i].size;
            _startLocalPositions[i] = _renderers[i].transform.localPosition;
        }
    }

    private bool IsEndpointRenderer(SpriteRenderer renderer)
    {
        if (renderer == null) return false;
        Transform rendererTransform = renderer.transform;
        return pointA != null && rendererTransform.IsChildOf(pointA)
            || pointB != null && rendererTransform.IsChildOf(pointB);
    }

    private void CacheHitbox()
    {
        if (_hitboxCached) return;
        if (hitbox == null) hitbox = GetComponent<BoxCollider2D>();
        if (hitbox == null) return;
        if (forceHitboxTrigger) hitbox.isTrigger = true;
        _hitboxStartSize = hitbox.size;
        _hitboxStartOffset = hitbox.offset;
        _hitboxCached = true;
    }

    private void SetHitboxActive(bool isActive)
    {
        CacheHitbox();
        if (hitbox != null)
            hitbox.enabled = isActive;
    }

    private void BeginDamage()
    {
        _hasLaunched = true;
        _damagedTargets.Clear();
        SetHitboxActive(true);
    }

    private void EnsureEndpointTransforms()
    {
        if (pointA == null) pointA = CreateEndpoint("Point A");
        if (pointB == null) pointB = CreateEndpoint("Point B");
    }

    private Transform CreateEndpoint(string endpointName)
    {
        Transform found = transform.Find(endpointName);
        if (found != null) return found;
        GameObject endpoint = new GameObject(endpointName);
        endpoint.hideFlags = HideFlags.DontSaveInEditor;
        endpoint.transform.SetParent(transform, false);
        return endpoint.transform;
    }

    private void UpdateHitbox(float distance)
    {
        CacheHitbox();
        if (hitbox == null) return;

        hitbox.size = new Vector2(Mathf.Max(0.0001f, distance), _hitboxStartSize.y);
        Vector3 midpoint = (pointA.localPosition + pointB.localPosition) * 0.5f;
        hitbox.offset = new Vector2(midpoint.x, _hitboxStartOffset.y);
    }

    private float GetEndpointDistance()
    {
        EnsureEndpointTransforms();
        return Vector2.Distance(pointA.position, pointB.position);
    }

    private float GetInitialDistance()
    {
        float distance = 0f;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            distance = Mathf.Max(distance, _startSizes[i].x);
        }
        return distance;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealTriggerDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealTriggerDamage(other);
    }

    private void TryDealTriggerDamage(Collider2D other)
    {
        if (!_hasLaunched) return;
        if (other == null) return;
        if (targetLayers.value != 0 && ((1 << other.gameObject.layer) & targetLayers.value) == 0) return;

        IDamagable target = other.GetComponent<IDamagable>() ?? other.GetComponentInParent<IDamagable>();
        if (target == null) return;
        if (damageEachTargetOnce && !_damagedTargets.Add(target)) return;

        Vector2 hitPoint = other.ClosestPoint(hitbox != null ? hitbox.bounds.center : transform.position);

        if (playerAttack != null)
        {
            playerAttack.DealDamage(target, hitPoint, GetChargedDamageMultiplier(), procCoefficient, gameObject);
        }
        else
        {
            float chargedDamage = damage * GetChargeScalar();
            target.TakeDamage(chargedDamage);
            FloatingDamagePool.Instance?.SpawnDamage(hitPoint, chargedDamage, false);
        }

        if (destroyOnDamage)
            Destroy(gameObject);
    }

    private float GetChargedDamageMultiplier()
    {
        return damageMultiplier * GetChargeScalar();
    }

    private float GetChargeScalar()
    {
        if (!scaleDamageWithCharge)
            return 1f;

        float threshold = Mathf.Clamp01(damageChargeThreshold);
        if (_launchCharge01 < threshold)
            return Mathf.Max(0f, belowThresholdDamageMultiplier);

        float upperCharge01 = Mathf.InverseLerp(threshold, 1f, Mathf.Clamp01(_launchCharge01));
        return Mathf.Lerp(
            Mathf.Max(0f, minChargeDamageMultiplier),
            Mathf.Max(0f, maxChargeDamageMultiplier),
            upperCharge01);
    }
}
