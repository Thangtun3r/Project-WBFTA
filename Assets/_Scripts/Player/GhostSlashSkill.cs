using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using _Scripts;

public class GhostSlashSkill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CollisionWeapon collisionWeapon;
    [SerializeField] private MouseFollower mouseFollower;
    [SerializeField] private SpriteRenderer sourceSpriteRenderer;
    [SerializeField] private GhostSlashRunner ghostRunnerPrefab;
    [SerializeField] private Movement playerMovement;

    [Header("Input")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q;

    [Header("Timing")]
    [SerializeField] private float slowTimeScale = 0.15f;
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private float replayDuration = 0.35f;
    [SerializeField] private float cooldown = 1.25f;
    [SerializeField] private float restoreDuration = 0.18f;
    [SerializeField] private Ease restoreEase = Ease.OutQuad;

    [Header("Path Recording")]
    [SerializeField] private float minPointDistance = 0.2f;
    [SerializeField] private bool enforceRecordedPointLimit = true;
    [SerializeField] private int maxRecordedPoints = 512;
    [SerializeField] private float replayAnchorDistance = 0.45f;
    [SerializeField] private float pivotSmoothing = 0.35f;

    [Header("Damage Sweep")]
    [SerializeField] private float damageRadius = 0.55f;

    [Header("Ghost Visual")]
    [SerializeField] private bool createRuntimeTrail = true;
    [SerializeField] private float trailTime = 0.2f;
    [SerializeField] private float trailStartWidth = 0.45f;
    [SerializeField] private float trailEndWidth = 0.05f;

    [Header("Slow Time Post Processing")]
    [SerializeField] private Volume slowTimeVolume;
    [SerializeField] private float slowTimeSaturationStart = 0f;
    [SerializeField] private float slowTimeSaturation = -100f;
    [SerializeField] private float slowTimeSaturationFadeDuration = 0.12f;
    [SerializeField] private float replayStartSaturation = 0f;

    [Header("Impact Frame")]
    [SerializeField] private float replayImpactFrameIntensity = 1f;

    private readonly List<GhostSlashRunner.RecordedPoint> _recordedPath = new List<GhostSlashRunner.RecordedPoint>(512);
    private readonly List<GhostSlashRunner.RecordedPoint> _replayPath = new List<GhostSlashRunner.RecordedPoint>(256);

    private Coroutine _executionRoutine;
    private bool _skillActive = true;
    private bool _timeScaleOverrideActive;
    private Tween _restoreTween;
    private Tween _saturationTween;
    private float _nextReadyUnscaledTime;
    private float _previousTimeScale = 1f;
    private float _previousFixedDeltaTime = 0.02f;
    private bool _movementFrozen;
    private ColorAdjustments _colorAdjustments;
    private bool _colorAdjustmentsResolved;
    private float _defaultSaturation;

    private void Awake()
    {
        if (collisionWeapon == null)
            collisionWeapon = GetComponent<CollisionWeapon>();

        if (mouseFollower == null)
            mouseFollower = FindFirstObjectByType<MouseFollower>();

        if (sourceSpriteRenderer == null)
            sourceSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (playerMovement == null)
            playerMovement = GetComponent<Movement>() ?? GetComponentInParent<Movement>();

        CacheColorAdjustments();
    }

    private void Update()
    {
        if (!_skillActive || _executionRoutine != null)
            return;

        if (collisionWeapon == null || collisionWeapon.PlayerAttack == null || mouseFollower == null)
            return;

        if (Time.unscaledTime < _nextReadyUnscaledTime)
            return;

        if (Input.GetKeyDown(triggerKey))
            _executionRoutine = StartCoroutine(ExecuteSkillRoutine());
    }

    private void OnDisable()
    {
        _restoreTween?.Kill();
        _restoreTween = null;
        _saturationTween?.Kill();
        _saturationTween = null;

        ReleaseMovementFreeze();
        OnScreenEffect.Instance?.ClearImpactFrame();

        RestoreSlowTimeSaturationImmediate();

        if (_timeScaleOverrideActive)
            RestoreTimeScale();

        if (_executionRoutine != null)
        {
            StopCoroutine(_executionRoutine);
            _executionRoutine = null;
        }
    }

    public void SetSkillActive(bool active)
    {
        _skillActive = active;
    }

    private IEnumerator ExecuteSkillRoutine()
    {
        _recordedPath.Clear();
        TryRecordCurrentPoint(force: true);

        ApplySlowTime();

        float recordStartTime = Time.unscaledTime;
        while (Time.unscaledTime - recordStartTime < slowDuration)
        {
            TryRecordCurrentPoint(force: false);
            yield return null;
        }

        TryRecordCurrentPoint(force: true);

        if (_recordedPath.Count >= 2)
        {
            BuildReplayPath();
            bool replayCompleted = false;
            ApplyMovementFreeze();
            TriggerReplayStartEffects();
            SpawnGhostRunner(replayDuration, () => replayCompleted = true);

            while (!replayCompleted)
                yield return null;

            OnScreenEffect.Instance?.ClearImpactFrame();
            ReleaseMovementFreeze();
            yield return RestoreTimeScaleGradually();
        }
        else
        {
            OnScreenEffect.Instance?.ClearImpactFrame();
            RestoreTimeScale();
        }

        _nextReadyUnscaledTime = Time.unscaledTime + cooldown;
        _executionRoutine = null;
    }

    private void TryRecordCurrentPoint(bool force)
    {
        Vector2 currentPoint = mouseFollower.GetWorldCursorPositionForFrame();
        float currentTime = Time.unscaledTime;

        if (_recordedPath.Count == 0)
        {
            AddRecordedPoint(new GhostSlashRunner.RecordedPoint(currentPoint, currentTime), force);
            return;
        }

        GhostSlashRunner.RecordedPoint lastPoint = _recordedPath[_recordedPath.Count - 1];
        float distance = Vector2.Distance(lastPoint.Position, currentPoint);
        float sampleDistance = Mathf.Max(0.01f, minPointDistance);

        if (!force && distance < sampleDistance)
            return;

        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / sampleDistance));
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 samplePoint = Vector2.Lerp(lastPoint.Position, currentPoint, t);
            float sampleTime = Mathf.Lerp(lastPoint.Time, currentTime, t);
            AddRecordedPoint(new GhostSlashRunner.RecordedPoint(samplePoint, sampleTime), force && i == steps);
        }
    }

    private void AddRecordedPoint(GhostSlashRunner.RecordedPoint point, bool force)
    {
        int effectiveMaxPoints = Mathf.Max(512, maxRecordedPoints);

        if (enforceRecordedPointLimit && !force && _recordedPath.Count >= effectiveMaxPoints)
            CompactRecordedPath(effectiveMaxPoints);

        if (enforceRecordedPointLimit && force && _recordedPath.Count >= effectiveMaxPoints)
        {
            _recordedPath[_recordedPath.Count - 1] = point;
        }
        else
        {
            _recordedPath.Add(point);
        }
    }

    private void CompactRecordedPath(int maxPoints)
    {
        if (_recordedPath.Count < maxPoints)
            return;

        int writeIndex = 1;
        for (int readIndex = 2; readIndex < _recordedPath.Count - 1; readIndex += 2)
        {
            _recordedPath[writeIndex] = _recordedPath[readIndex];
            writeIndex++;
        }

        _recordedPath[writeIndex] = _recordedPath[_recordedPath.Count - 1];
        writeIndex++;

        if (writeIndex < _recordedPath.Count)
            _recordedPath.RemoveRange(writeIndex, _recordedPath.Count - writeIndex);
    }

    private void SpawnGhostRunner(float replayDuration, System.Action onCompleted)
    {
        GhostSlashRunner runner = ghostRunnerPrefab != null
            ? Instantiate(ghostRunnerPrefab, Vector3.zero, Quaternion.identity)
            : CreateRuntimeRunner();

        runner.Initialize(
            _replayPath.ToArray(),
            damageRadius,
            collisionWeapon.TargetLayers,
            collisionWeapon.PlayerAttack,
            collisionWeapon.DamageMultiplier,
            collisionWeapon.ProcCoefficient,
            collisionWeapon.gameObject,
            sourceSpriteRenderer,
            replayDuration,
            onCompleted);
    }

    private void BuildReplayPath()
    {
        _replayPath.Clear();
        if (_recordedPath.Count == 0)
            return;

        float anchorDistance = Mathf.Max(minPointDistance, replayAnchorDistance);
        _replayPath.Add(_recordedPath[0]);

        for (int i = 1; i < _recordedPath.Count - 1; i++)
        {
            GhostSlashRunner.RecordedPoint candidate = _recordedPath[i];
            GhostSlashRunner.RecordedPoint lastAnchor = _replayPath[_replayPath.Count - 1];

            if (Vector2.Distance(lastAnchor.Position, candidate.Position) < anchorDistance)
                continue;

            _replayPath.Add(candidate);
        }

        GhostSlashRunner.RecordedPoint lastPoint = _recordedPath[_recordedPath.Count - 1];
        if (_replayPath.Count == 0 || _replayPath[_replayPath.Count - 1].Position != lastPoint.Position)
            _replayPath.Add(lastPoint);

        if (_replayPath.Count < 2 && _recordedPath.Count > 1)
        {
            _replayPath.Clear();
            _replayPath.Add(_recordedPath[0]);
            _replayPath.Add(lastPoint);
        }

        SmoothReplayPivots();
    }

    private void SmoothReplayPivots()
    {
        if (_replayPath.Count < 3)
            return;

        float smoothing = Mathf.Clamp01(pivotSmoothing);
        if (smoothing <= 0f)
            return;

        for (int i = 1; i < _replayPath.Count - 1; i++)
        {
            GhostSlashRunner.RecordedPoint previous = _replayPath[i - 1];
            GhostSlashRunner.RecordedPoint current = _replayPath[i];
            GhostSlashRunner.RecordedPoint next = _replayPath[i + 1];

            Vector2 smoothedPosition = Vector2.Lerp(
                current.Position,
                (previous.Position + next.Position) * 0.5f,
                smoothing);

            _replayPath[i] = new GhostSlashRunner.RecordedPoint(smoothedPosition, current.Time);
        }
    }

    private GhostSlashRunner CreateRuntimeRunner()
    {
        GameObject runnerObject = new GameObject("Ghost Slash Runner");
        GhostSlashRunner runner = runnerObject.AddComponent<GhostSlashRunner>();

        if (createRuntimeTrail)
        {
            TrailRenderer trail = runnerObject.GetComponent<TrailRenderer>() ?? runnerObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = trailStartWidth;
            trail.endWidth = trailEndWidth;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
        }

        CircleCollider2D hitCollider = runnerObject.GetComponent<CircleCollider2D>() ?? runnerObject.AddComponent<CircleCollider2D>();
        hitCollider.isTrigger = true;
        hitCollider.radius = damageRadius;

        return runner;
    }

    private void ApplyMovementFreeze()
    {
        if (_movementFrozen)
            return;

        playerMovement?.SetMovementFrozen(true);
        _movementFrozen = true;
    }

    private void ReleaseMovementFreeze()
    {
        if (!_movementFrozen)
            return;

        playerMovement?.SetMovementFrozen(false);
        _movementFrozen = false;
    }

    private void ApplySlowTime()
    {
        if (_timeScaleOverrideActive)
            return;

        _restoreTween?.Kill();
        _restoreTween = null;
        _previousTimeScale = Time.timeScale;
        _previousFixedDeltaTime = Time.fixedDeltaTime;
        _timeScaleOverrideActive = true;

        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = _previousFixedDeltaTime * slowTimeScale;
        SetSlowTimeSaturationImmediate(slowTimeSaturationStart);
        TweenSlowTimeSaturation(slowTimeSaturation);
    }

    private IEnumerator RestoreTimeScaleGradually()
    {
        if (!_timeScaleOverrideActive)
            yield break;

        float duration = Mathf.Max(0f, restoreDuration);
        if (duration <= Mathf.Epsilon)
        {
            RestoreTimeScale();
            yield break;
        }

        float startScale = Time.timeScale;
        float targetScale = _previousTimeScale;
        bool completed = false;

        TweenSlowTimeSaturation(_defaultSaturation);
        _restoreTween?.Kill();
        _restoreTween = DOTween.To(() => startScale, value =>
            {
                startScale = value;
                Time.timeScale = value;
                Time.fixedDeltaTime = _previousFixedDeltaTime * value;
            }, targetScale, duration)
            .SetEase(restoreEase)
            .SetUpdate(true)
            .OnComplete(() => completed = true);

        while (!completed)
            yield return null;

        _restoreTween = null;
        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!_timeScaleOverrideActive)
            return;

        _restoreTween?.Kill();
        _restoreTween = null;
        Time.timeScale = _previousTimeScale;
        Time.fixedDeltaTime = _previousFixedDeltaTime;
        _timeScaleOverrideActive = false;
    }

    private void CacheColorAdjustments()
    {
        if (_colorAdjustmentsResolved)
            return;

        _colorAdjustmentsResolved = true;
        if (slowTimeVolume == null)
            return;

        VolumeProfile profile = slowTimeVolume.profile != null ? slowTimeVolume.profile : slowTimeVolume.sharedProfile;
        if (profile == null)
            return;

        if (!profile.TryGet(out _colorAdjustments))
            return;

        _defaultSaturation = _colorAdjustments.saturation.value;
    }

    private void TweenSlowTimeSaturation(float targetSaturation)
    {
        CacheColorAdjustments();
        if (_colorAdjustments == null)
            return;

        _saturationTween?.Kill();

        float currentSaturation = _colorAdjustments.saturation.value;
        float duration = Mathf.Max(0f, slowTimeSaturationFadeDuration);
        if (duration <= Mathf.Epsilon)
        {
            _colorAdjustments.saturation.Override(targetSaturation);
            return;
        }

        _saturationTween = DOTween.To(
                () => currentSaturation,
                value =>
                {
                    currentSaturation = value;
                    _colorAdjustments.saturation.Override(value);
                },
                targetSaturation,
                duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnKill(() => _saturationTween = null)
            .OnComplete(() => _saturationTween = null);
    }

    private void RestoreSlowTimeSaturationImmediate()
    {
        CacheColorAdjustments();
        if (_colorAdjustments == null)
            return;

        _saturationTween?.Kill();
        _saturationTween = null;
        _colorAdjustments.saturation.Override(_defaultSaturation);
    }

    private void TriggerReplayStartEffects()
    {
        OnScreenEffect.Instance?.ShakeHUD();
        OnScreenEffect.Instance?.SetImpactFrame(replayImpactFrameIntensity);
        SetSlowTimeSaturationImmediate(replayStartSaturation);
    }

    private void SetSlowTimeSaturationImmediate(float saturation)
    {
        CacheColorAdjustments();
        if (_colorAdjustments == null)
            return;

        _saturationTween?.Kill();
        _saturationTween = null;
        _colorAdjustments.saturation.Override(saturation);
    }
}
