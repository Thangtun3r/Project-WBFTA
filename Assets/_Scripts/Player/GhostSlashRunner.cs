using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class GhostSlashRunner : MonoBehaviour
{
    public struct RecordedPoint
    {
        public Vector2 Position { get; private set; }
        public float Time { get; private set; }

        public RecordedPoint(Vector2 position, float time)
        {
            Position = position;
            Time = time;
        }
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private CircleCollider2D hitCollider;
    [SerializeField] private float fadeInDuration = 0.05f;
    [SerializeField] private float fadeOutDuration = 0.08f;
    [SerializeField] private float maxMovementSpeed = 18f;
    [SerializeField] private bool useSplineReplay = true;
    [SerializeField] private int splineSubdivisions = 6;
    [Header("Afterimage Trail")]
    [SerializeField] private int afterimageCount = 5;
    [SerializeField] private float afterimageTrailLength = 0.18f;
    [SerializeField] private float afterimageAlphaMultiplier = 0.65f;
    [Header("Damage")]
    [SerializeField] private float repeatHitInterval = 0.08f;

    private readonly Collider2D[] _overlapResults = new Collider2D[32];
    private readonly Dictionary<IDamagable, float> _lastTargetHitTimes = new Dictionary<IDamagable, float>();
    private readonly List<SpriteRenderer> _afterimageRenderers = new List<SpriteRenderer>();

    private IReadOnlyList<RecordedPoint> _path;
    private PlayerAttack _playerAttack;
    private GameObject _damageSource;
    private LayerMask _targetLayers;
    private float _fixedReplayDuration;
    private float _damageMultiplier;
    private float _procCoefficient;
    private float _damageRadius;
    private bool _useTargetLayerFilter;
    private float _targetSpriteAlpha;
    private Action _onCompleted;
    private readonly List<RecordedPoint> _evaluatedPath = new List<RecordedPoint>(512);

    public float Initialize(
        IReadOnlyList<RecordedPoint> path,
        float damageRadius,
        LayerMask targetLayers,
        PlayerAttack playerAttack,
        float damageMultiplier,
        float procCoefficient,
        GameObject damageSource,
        SpriteRenderer sourceSpriteRenderer,
        float fixedReplayDuration = -1f,
        Action onCompleted = null)
    {
        _path = path;
        _fixedReplayDuration = Mathf.Max(0.01f, fixedReplayDuration);
        _damageRadius = Mathf.Max(0.05f, damageRadius);
        _targetLayers = targetLayers;
        _playerAttack = playerAttack;
        _damageMultiplier = damageMultiplier;
        _procCoefficient = procCoefficient;
        _damageSource = damageSource;
        _useTargetLayerFilter = targetLayers.value != 0;
        _onCompleted = onCompleted;

        EnsureComponents();
        hitCollider.radius = _damageRadius;
        CopyVisuals(sourceSpriteRenderer);

        StopAllCoroutines();
        StartCoroutine(ReplayRoutine());
        return CalculateTotalPlaybackDuration();
    }

    private void EnsureComponents()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();

        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>() ?? gameObject.AddComponent<TrailRenderer>();

        if (hitCollider == null)
        {
            hitCollider = GetComponent<CircleCollider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
            hitCollider.isTrigger = true;
        }

        EnsureAfterimages();
    }

    private void CopyVisuals(SpriteRenderer sourceSpriteRenderer)
    {
        if (sourceSpriteRenderer == null || spriteRenderer == null)
            return;

        Color color = sourceSpriteRenderer.color;
        _targetSpriteAlpha = color.a * 0.55f;
        color.a = 0f;

        spriteRenderer.sprite = sourceSpriteRenderer.sprite;
        spriteRenderer.color = color;
        spriteRenderer.flipX = sourceSpriteRenderer.flipX;
        spriteRenderer.flipY = sourceSpriteRenderer.flipY;
        spriteRenderer.sortingLayerID = sourceSpriteRenderer.sortingLayerID;
        spriteRenderer.sortingOrder = sourceSpriteRenderer.sortingOrder - 1;
        transform.localScale = sourceSpriteRenderer.transform.lossyScale;

        for (int i = 0; i < _afterimageRenderers.Count; i++)
        {
            SpriteRenderer afterimage = _afterimageRenderers[i];
            if (afterimage == null)
                continue;

            Color afterimageColor = color;
            afterimageColor.a = 0f;

            afterimage.sprite = sourceSpriteRenderer.sprite;
            afterimage.color = afterimageColor;
            afterimage.flipX = sourceSpriteRenderer.flipX;
            afterimage.flipY = sourceSpriteRenderer.flipY;
            afterimage.sortingLayerID = sourceSpriteRenderer.sortingLayerID;
            afterimage.sortingOrder = spriteRenderer.sortingOrder - (i + 1);
            afterimage.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator ReplayRoutine()
    {
        if (_path == null || _path.Count < 2)
        {
            CompleteAndDestroy();
            yield break;
        }

        IReadOnlyList<RecordedPoint> replayPath = BuildEvaluatedPath(_path);
        float totalLength = CalculateTotalLength(replayPath);
        float recordedDuration = CalculateRecordedDuration(replayPath);
        if (totalLength <= Mathf.Epsilon || recordedDuration <= Mathf.Epsilon)
        {
            CompleteAndDestroy();
            yield break;
        }

        float replayDuration = GetReplayDuration();
        float fadeInTime = Mathf.Max(0f, fadeInDuration);
        float fadeOutTime = Mathf.Max(0f, fadeOutDuration);
        float movementDuration = CalculateMovementDuration(replayDuration, fadeInTime, fadeOutTime, totalLength, maxMovementSpeed);
        float[] cumulativeLengths = BuildCumulativeLengths(replayPath);

        _lastTargetHitTimes.Clear();
        transform.position = new Vector3(replayPath[0].Position.x, replayPath[0].Position.y, transform.position.z);
        UpdateAfterimages(0f, cumulativeLengths, totalLength, replayPath);

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = false;
        }

        yield return null;

        if (trailRenderer != null)
            trailRenderer.emitting = true;

        if (fadeInTime > 0f)
            yield return FadeSpriteAlpha(0f, _targetSpriteAlpha, fadeInTime);
        else
            SetSpriteAlpha(_targetSpriteAlpha, includeAfterimages: true);

        Vector2 previousPosition = replayPath[0].Position;
        float elapsed = 0f;
        while (elapsed < movementDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = movementDuration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / movementDuration);
            Vector2 currentPosition = GetPositionAtNormalizedDistance(replayPath, cumulativeLengths, totalLength, t);

            transform.position = new Vector3(currentPosition.x, currentPosition.y, transform.position.z);
            UpdateAfterimages(t, cumulativeLengths, totalLength, replayPath);
            SweepDamage(previousPosition, currentPosition);
            previousPosition = currentPosition;

            yield return null;
        }

        Vector2 finalPosition = replayPath[replayPath.Count - 1].Position;
        transform.position = new Vector3(finalPosition.x, finalPosition.y, transform.position.z);
        UpdateAfterimages(1f, cumulativeLengths, totalLength, replayPath);
        SweepDamage(previousPosition, finalPosition);

        if (fadeOutTime > 0f)
            yield return FadeSpriteAlpha(_targetSpriteAlpha, 0f, fadeOutTime);

        CompleteAndDestroy();
    }

    private void SweepDamage(Vector2 start, Vector2 end)
    {
        if (_playerAttack == null)
            return;

        float distance = Vector2.Distance(start, end);
        float stepSize = Mathf.Max(_damageRadius * 0.5f, 0.05f);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / stepSize));

        for (int i = 0; i <= steps; i++)
        {
            Vector2 samplePoint = Vector2.Lerp(start, end, i / (float)steps);
            int hitCount = _useTargetLayerFilter
                ? Physics2D.OverlapCircleNonAlloc(samplePoint, _damageRadius, _overlapResults, _targetLayers)
                : Physics2D.OverlapCircleNonAlloc(samplePoint, _damageRadius, _overlapResults);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D hit = _overlapResults[hitIndex];
                if (hit == null)
                    continue;

                IDamagable target = hit.GetComponent<IDamagable>() ?? hit.GetComponentInParent<IDamagable>();
                if (target == null || !CanHitTarget(target))
                    continue;

                _playerAttack.DealDamage(target, samplePoint, _damageMultiplier, _procCoefficient, _damageSource);
                _lastTargetHitTimes[target] = Time.unscaledTime;
            }
        }
    }

    private bool CanHitTarget(IDamagable target)
    {
        float interval = Mathf.Max(0f, repeatHitInterval);
        return !_lastTargetHitTimes.TryGetValue(target, out float lastHitTime)
            || Time.unscaledTime >= lastHitTime + interval;
    }

    private static float CalculateTotalLength(IReadOnlyList<RecordedPoint> path)
    {
        float totalLength = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            totalLength += Vector2.Distance(path[i - 1].Position, path[i].Position);
        }

        return totalLength;
    }

    private static float CalculateRecordedDuration(IReadOnlyList<RecordedPoint> path)
    {
        if (path.Count < 2)
            return 0f;

        return Mathf.Max(0.01f, path[path.Count - 1].Time - path[0].Time);
    }

    private static float[] BuildCumulativeLengths(IReadOnlyList<RecordedPoint> path)
    {
        float[] cumulativeLengths = new float[path.Count];
        cumulativeLengths[0] = 0f;

        for (int i = 1; i < path.Count; i++)
        {
            cumulativeLengths[i] = cumulativeLengths[i - 1] + Vector2.Distance(path[i - 1].Position, path[i].Position);
        }

        return cumulativeLengths;
    }

    private static Vector2 GetPositionAtNormalizedDistance(
        IReadOnlyList<RecordedPoint> path,
        float[] cumulativeLengths,
        float totalLength,
        float normalizedDistance)
    {
        if (path.Count == 0)
            return Vector2.zero;

        if (path.Count == 1 || totalLength <= Mathf.Epsilon)
            return path[0].Position;

        float targetDistance = totalLength * Mathf.Clamp01(normalizedDistance);
        for (int i = 1; i < cumulativeLengths.Length; i++)
        {
            if (targetDistance > cumulativeLengths[i])
                continue;

            float segmentStartDistance = cumulativeLengths[i - 1];
            float segmentLength = cumulativeLengths[i] - segmentStartDistance;
            float t = segmentLength <= Mathf.Epsilon
                ? 1f
                : Mathf.Clamp01((targetDistance - segmentStartDistance) / segmentLength);

            return Vector2.Lerp(path[i - 1].Position, path[i].Position, t);
        }

        return path[path.Count - 1].Position;
    }

    private float CalculateTotalPlaybackDuration()
    {
        IReadOnlyList<RecordedPoint> replayPath = BuildEvaluatedPath(_path);
        if (replayPath == null || replayPath.Count < 2)
            return 0f;

        float replayDuration = GetReplayDuration();
        float fadeInTime = Mathf.Max(0f, fadeInDuration);
        float fadeOutTime = Mathf.Max(0f, fadeOutDuration);
        float totalLength = CalculateTotalLength(replayPath);
        float movementDuration = CalculateMovementDuration(replayDuration, fadeInTime, fadeOutTime, totalLength, maxMovementSpeed);

        return fadeInTime + movementDuration + fadeOutTime;
    }

    private static float CalculateMovementDuration(
        float replayDuration,
        float fadeInTime,
        float fadeOutTime,
        float totalLength,
        float maxMovementSpeed)
    {
        float requestedDuration = replayDuration - fadeInTime - fadeOutTime;
        float baseDuration = requestedDuration > 0f ? requestedDuration : replayDuration;
        float clampedSpeed = Mathf.Max(0.01f, maxMovementSpeed);
        float minimumDurationForSpeedCap = totalLength / clampedSpeed;

        return Mathf.Max(baseDuration, minimumDurationForSpeedCap);
    }

    private float GetReplayDuration()
    {
        return _fixedReplayDuration;
    }

    private IEnumerator FadeSpriteAlpha(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        SetSpriteAlpha(fromAlpha, includeAfterimages: true);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
            SetSpriteAlpha(Mathf.Lerp(fromAlpha, toAlpha, t), includeAfterimages: true);
            yield return null;
        }

        SetSpriteAlpha(toAlpha, includeAfterimages: true);
    }

    private void SetSpriteAlpha(float alpha, bool includeAfterimages)
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;

        if (!includeAfterimages)
            return;

        for (int i = 0; i < _afterimageRenderers.Count; i++)
        {
            SpriteRenderer afterimage = _afterimageRenderers[i];
            if (afterimage == null)
                continue;

            float falloff = 1f - ((i + 1f) / (_afterimageRenderers.Count + 1f));
            Color afterimageColor = afterimage.color;
            afterimageColor.a = alpha * afterimageAlphaMultiplier * falloff;
            afterimage.color = afterimageColor;
        }
    }

    private void EnsureAfterimages()
    {
        int desiredCount = Mathf.Max(0, afterimageCount);
        while (_afterimageRenderers.Count < desiredCount)
        {
            GameObject afterimageObject = new GameObject($"Ghost Afterimage {_afterimageRenderers.Count + 1}");
            afterimageObject.transform.SetParent(transform, false);
            SpriteRenderer afterimage = afterimageObject.AddComponent<SpriteRenderer>();
            _afterimageRenderers.Add(afterimage);
        }

        while (_afterimageRenderers.Count > desiredCount)
        {
            int lastIndex = _afterimageRenderers.Count - 1;
            SpriteRenderer afterimage = _afterimageRenderers[lastIndex];
            if (afterimage != null)
                Destroy(afterimage.gameObject);

            _afterimageRenderers.RemoveAt(lastIndex);
        }
    }

    private void UpdateAfterimages(float normalizedDistance, float[] cumulativeLengths, float totalLength, IReadOnlyList<RecordedPoint> replayPath)
    {
        if (_afterimageRenderers.Count == 0)
            return;

        float trailLength = Mathf.Max(0.01f, afterimageTrailLength);
        for (int i = 0; i < _afterimageRenderers.Count; i++)
        {
            SpriteRenderer afterimage = _afterimageRenderers[i];
            if (afterimage == null)
                continue;

            float lag = trailLength * ((i + 1f) / (_afterimageRenderers.Count + 1f));
            float afterimageDistance = normalizedDistance - lag;
            SetAfterimageVisible(afterimage, afterimageDistance >= 0f);

            if (afterimageDistance < 0f)
                continue;

            Vector2 worldPosition = GetPositionAtNormalizedDistance(replayPath, cumulativeLengths, totalLength, afterimageDistance);
            afterimage.transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }
    }

    private IReadOnlyList<RecordedPoint> BuildEvaluatedPath(IReadOnlyList<RecordedPoint> sourcePath)
    {
        _evaluatedPath.Clear();
        if (sourcePath == null || sourcePath.Count == 0)
            return _evaluatedPath;

        if (!useSplineReplay || sourcePath.Count < 3)
        {
            for (int i = 0; i < sourcePath.Count; i++)
                _evaluatedPath.Add(sourcePath[i]);

            return _evaluatedPath;
        }

        int subdivisions = Mathf.Max(2, splineSubdivisions);
        _evaluatedPath.Add(sourcePath[0]);

        for (int i = 0; i < sourcePath.Count - 1; i++)
        {
            Vector2 p0 = sourcePath[Mathf.Max(i - 1, 0)].Position;
            Vector2 p1 = sourcePath[i].Position;
            Vector2 p2 = sourcePath[i + 1].Position;
            Vector2 p3 = sourcePath[Mathf.Min(i + 2, sourcePath.Count - 1)].Position;

            float t1 = sourcePath[i].Time;
            float t2 = sourcePath[i + 1].Time;

            for (int step = 1; step <= subdivisions; step++)
            {
                float t = step / (float)subdivisions;
                Vector2 position = EvaluateCatmullRom(p0, p1, p2, p3, t);
                float time = Mathf.Lerp(t1, t2, t);
                _evaluatedPath.Add(new RecordedPoint(position, time));
            }
        }

        return _evaluatedPath;
    }

    private static Vector2 EvaluateCatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private static void SetAfterimageVisible(SpriteRenderer afterimage, bool visible)
    {
        if (afterimage == null)
            return;

        afterimage.enabled = visible;
    }

    private void CompleteAndDestroy()
    {
        Action completed = _onCompleted;
        _onCompleted = null;
        completed?.Invoke();
        Destroy(gameObject);
    }
}
