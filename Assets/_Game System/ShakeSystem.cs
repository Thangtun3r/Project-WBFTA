using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ShakeSystem : MonoBehaviour
{
    private enum ShakeSpace
    {
        WorldPosition,
        AnchoredUI
    }

    private class ShakeState
    {
        public Transform Target;
        public RectTransform RectTarget;
        public ShakeSpace Space;
        public Vector3 PreviousWorldOffset;
        public Vector2 PreviousAnchoredOffset;
        public float Strength;
        public float Duration;
        public float Timer;
        public float Frequency;
        public float SeedX;
        public float SeedY;
        public float SampleTime;
    }

    [Header("Defaults")]
    [SerializeField] private float defaultStrength = 0.25f;
    [SerializeField] private float defaultDuration = 0.15f;
    [SerializeField] private float defaultFrequency = 35f;

    private static ShakeSystem instance;
    private readonly List<ShakeState> activeShakes = new List<ShakeState>();

    public static void ShakeCamera()
    {
        ShakeSystem shaker = GetOrCreate();
        ShakeCamera(shaker.defaultStrength, shaker.defaultDuration, shaker.defaultFrequency);
    }

    public static void ShakeCamera(float strength, float duration)
    {
        ShakeSystem shaker = GetOrCreate();
        ShakeCamera(strength, duration, shaker.defaultFrequency);
    }

    public static void ShakeCamera(float strength, float duration, float frequency)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        ShakeTarget(mainCamera.transform, strength, duration, frequency);
    }

    public static void ShakeTarget(Transform target)
    {
        if (target == null)
            return;

        ShakeSystem shaker = GetOrCreate();
        ShakeTarget(target, shaker.defaultStrength, shaker.defaultDuration, shaker.defaultFrequency);
    }

    public static void ShakeTarget(Transform target, float strength, float duration)
    {
        if (target == null)
            return;

        ShakeSystem shaker = GetOrCreate();
        ShakeTarget(target, strength, duration, shaker.defaultFrequency);
    }

    public static void ShakeTarget(Transform target, float strength, float duration, float frequency)
    {
        if (target == null)
            return;

        GetOrCreate().AddShake(target, null, ShakeSpace.WorldPosition, strength, duration, frequency);
    }

    public static void ShakeUI(RectTransform target)
    {
        if (target == null)
            return;

        ShakeSystem shaker = GetOrCreate();
        ShakeUI(target, shaker.defaultStrength, shaker.defaultDuration, shaker.defaultFrequency);
    }

    public static void ShakeUI(RectTransform target, float strength, float duration)
    {
        if (target == null)
            return;

        ShakeSystem shaker = GetOrCreate();
        ShakeUI(target, strength, duration, shaker.defaultFrequency);
    }

    public static void ShakeUI(RectTransform target, float strength, float duration, float frequency)
    {
        if (target == null)
            return;

        GetOrCreate().AddShake(target, target, ShakeSpace.AnchoredUI, strength, duration, frequency);
    }

    private static ShakeSystem GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<ShakeSystem>();
        if (instance != null)
            return instance;

        GameObject shakerObject = new GameObject("Shake System");
        instance = shakerObject.AddComponent<ShakeSystem>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDisable()
    {
        ClearOffsets();
    }

    private void LateUpdate()
    {
        for (int i = activeShakes.Count - 1; i >= 0; i--)
        {
            ShakeState shake = activeShakes[i];
            RemovePreviousOffset(shake);

            if (shake.Target == null)
            {
                activeShakes.RemoveAt(i);
                continue;
            }

            shake.Timer -= Time.unscaledDeltaTime;
            if (shake.Timer <= 0f)
            {
                activeShakes.RemoveAt(i);
                continue;
            }

            shake.SampleTime += Time.unscaledDeltaTime * shake.Frequency;
            ApplyShake(shake);
        }
    }

    private void AddShake(Transform target, RectTransform rectTarget, ShakeSpace space, float strength, float duration, float frequency)
    {
        ShakeState shake = FindShake(target, space);
        if (shake == null)
        {
            shake = new ShakeState
            {
                Target = target,
                RectTarget = rectTarget,
                Space = space
            };
            activeShakes.Add(shake);
        }

        shake.Strength = Mathf.Max(shake.Strength, Mathf.Max(0f, strength));
        shake.Duration = Mathf.Max(shake.Duration, Mathf.Max(0.001f, duration));
        shake.Timer = Mathf.Max(shake.Timer, Mathf.Max(0f, duration));
        shake.Frequency = Mathf.Max(0.001f, frequency);
        shake.SeedX = Random.value * 100f;
        shake.SeedY = Random.value * 100f;
        shake.SampleTime = 0f;
    }

    private ShakeState FindShake(Transform target, ShakeSpace space)
    {
        for (int i = 0; i < activeShakes.Count; i++)
        {
            ShakeState shake = activeShakes[i];
            if (shake.Target == target && shake.Space == space)
                return shake;
        }

        return null;
    }

    private void ApplyShake(ShakeState shake)
    {
        float normalizedTime = shake.Duration > 0f ? Mathf.Clamp01(shake.Timer / shake.Duration) : 0f;
        float damper = normalizedTime * normalizedTime;
        Vector2 offset = new Vector2(
            (Mathf.PerlinNoise(shake.SeedX, shake.SampleTime) - 0.5f) * 2f,
            (Mathf.PerlinNoise(shake.SeedY, shake.SampleTime) - 0.5f) * 2f) * (shake.Strength * damper);

        if (shake.Space == ShakeSpace.AnchoredUI && shake.RectTarget != null)
        {
            shake.PreviousAnchoredOffset = offset;
            shake.RectTarget.anchoredPosition += offset;
            return;
        }

        shake.PreviousWorldOffset = new Vector3(offset.x, offset.y, 0f);
        shake.Target.position += shake.PreviousWorldOffset;
    }

    private void RemovePreviousOffset(ShakeState shake)
    {
        if (shake.Space == ShakeSpace.AnchoredUI && shake.RectTarget != null)
        {
            if (shake.PreviousAnchoredOffset == Vector2.zero)
                return;

            shake.RectTarget.anchoredPosition -= shake.PreviousAnchoredOffset;
            shake.PreviousAnchoredOffset = Vector2.zero;
            return;
        }

        if (shake.PreviousWorldOffset == Vector3.zero || shake.Target == null)
            return;

        shake.Target.position -= shake.PreviousWorldOffset;
        shake.PreviousWorldOffset = Vector3.zero;
    }

    private void ClearOffsets()
    {
        for (int i = activeShakes.Count - 1; i >= 0; i--)
        {
            RemovePreviousOffset(activeShakes[i]);
        }

        activeShakes.Clear();
    }
}
