using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StageTransitionManager : MonoBehaviour
{
    public static event Action OnNextStageTriggered;

    [Header("References")]
    [SerializeField] private Image transitionOverlay; 
    [SerializeField] private SpriteRenderer targetSprite; 

    [Header("Timing Settings")]
    [SerializeField] private float fadeInSpeed = 0.5f;
    [SerializeField] private float holdDuration = 1.0f;
    [SerializeField] private float fadeOutSpeed = 0.5f;

    private int _lastColorIndex = -1;
    private bool _isTransitioning = false; // The "Lock" variable
    
    private readonly Color[] _cycleColors = {
        new Color(1f, 0f, 0f),      // Red
        new Color(0f, 0f, 1f),      // Blue
        new Color(1f, 0.5f, 0f),    // Orange
        new Color(0.5f, 0f, 0.5f),  // Purple
        new Color(1f, 0.41f, 0.7f), // Pink
        new Color(1f, 0.92f, 0.01f),// Yellow
        new Color(0f, 1f, 0f)       // Green
    };

    void OnEnable() => VideoCharging.OnNextLevelToggled += HandleNextLevel;
    void OnDisable() => VideoCharging.OnNextLevelToggled -= HandleNextLevel;

    private void HandleNextLevel()
    {
        // If we are already fading, ignore any further input/triggers
        if (_isTransitioning) return;

        OnNextStageTriggered?.Invoke();
        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        _isTransitioning = true; // Lock the method

        // 1. FADE IN
        yield return StartCoroutine(FadeGuiAlpha(0, 1, fadeInSpeed));

        // --- MIDDLE CHANGE ---
        if (targetSprite != null)
        {
            int nextIndex;
            do {
                nextIndex = UnityEngine.Random.Range(0, _cycleColors.Length);
            } while (nextIndex == _lastColorIndex);

            _lastColorIndex = nextIndex;

            Color newColor = _cycleColors[nextIndex];
            newColor.a = targetSprite.color.a; 
            targetSprite.color = newColor;
        }

        yield return new WaitForSeconds(holdDuration);

        // 2. FADE OUT
        yield return StartCoroutine(FadeGuiAlpha(1, 0, fadeOutSpeed));

        _isTransitioning = false; // Unlock the method
    }

    private IEnumerator FadeGuiAlpha(float start, float end, float duration)
    {
        float elapsed = 0;
        Color c = transitionOverlay.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(start, end, elapsed / duration);
            transitionOverlay.color = c;
            yield return null;
        }
        
        c.a = end;
        transitionOverlay.color = c;
    }
}