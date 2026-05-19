using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;

public class OnScreenEffect : MonoBehaviour
{
    public static OnScreenEffect Instance { get; private set; }
    
    [Header("UI Shake Settings")]
    [Tooltip("Assign the HUD/UI RectTransform you want to shake alongside the camera.")]
    [SerializeField] private RectTransform uiTargetToShake;

    [Header("Cinemachine Settings")]
    [Tooltip("Attach a CinemachineImpulseSource component to this GameObject and assign it here.")]
    [SerializeField] private CinemachineImpulseSource cameraImpulseSource;
    
    [Tooltip("Direction and base force of the Cinemachine Impulse.")]
    [SerializeField] private Vector3 impulseVelocity = new Vector3(1f, 1f, 0f);

    [Tooltip("Multiplier to balance Cinemachine shake strength vs DOTween strength.")]
    [SerializeField] private float cinemachineForceMultiplier = 1f;

    [Header("Default Shake Settings")]
    [SerializeField] private float defaultShakeStrength = 0.5f;
    [SerializeField] private float defaultShakeDuration = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Shakes a specific object's transform over time.
    /// </summary>
    public void ShakeObject(Transform target, float strength, float duration)
    {
        if (target != null)
        {
            // Complete any active tween to return to original position before shaking again
            target.DOComplete();
            // Using DOTween's DOShakePosition
            target.DOShakePosition(duration, strength);
        }
    }

    /// <summary>
    /// Triggers a camera shake via Cinemachine and shakes the assigned HUD using default values.
    /// </summary>
    public void ShakeHUD()
    {
        ShakeHUD(defaultShakeStrength, defaultShakeDuration);
    }

    /// <summary>
    /// Triggers a camera shake via Cinemachine and shakes the HUD with a custom force.
    /// </summary>
    public void ShakeHUD(float strength)
    {
        ShakeHUD(strength, defaultShakeDuration);
    }

    /// <summary>
    /// Triggers a camera shake via Cinemachine and shakes the HUD with custom duration and strength.
    /// </summary>
    public void ShakeHUD(float strength, float duration)
    {
        // 1. Shake the Camera
        ShakeCamera(strength, duration);

        // 2. Shake the UI/HUD simultaneously using DOTween
        if (uiTargetToShake != null)
        {
            uiTargetToShake.DOComplete();
            uiTargetToShake.DOShakeAnchorPos(duration, strength);
        }
    }

    /// <summary>
    /// Shakes the main camera using Cinemachine Impulse or DOTween fallback.
    /// </summary>
    public void ShakeCamera(float strength, float duration)
    {
        if (cameraImpulseSource != null)
        {
            // Generates an impulse in the assigned direction, scaled by the strength and multiplier
            cameraImpulseSource.GenerateImpulseWithVelocity(impulseVelocity * (strength * cinemachineForceMultiplier));
        }
        else if (Camera.main != null)
        {
            Camera.main.transform.DOComplete();
            Camera.main.transform.DOShakePosition(duration, strength);
        }
    }
}
