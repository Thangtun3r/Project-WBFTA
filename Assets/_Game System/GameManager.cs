using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float TimeElapsed { get; private set; }
    public int CurrentLevel { get; private set; }
    public bool IsGameActive { get; private set; }

    [Header("Difficulty Scaling")]
    [SerializeField] private float baseDifficulty = 1f;
    [SerializeField] private float timeScalar = 0.050f; // How much difficulty increases per second
    [SerializeField] private float stageMultiplier = 1.15f; // The 15% "bump" per level
    
    // This is the value your enemy spawners should use to scale HP/Damage
    public float DifficultyCoefficient { get; private set; }

    [SerializeField] private float levelUpInterval = 30f;

    public static event Action<float> OnTimeUpdated;
    public static event Action<int> OnLevelChanged;
    public static event Action<float> OnDifficultyChanged;

    private float _timeSinceLastLevelUp;
    private float _currentStageMultiplier = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (IsGameActive)
        {
            TimeElapsed += Time.deltaTime;
            _timeSinceLastLevelUp += Time.deltaTime;
            
            // Calculate RoR2-style scaling: (Base + (Time * Scalar)) * (Multiplier ^ Level)
            CalculateDifficulty();

            OnTimeUpdated?.Invoke(TimeElapsed);

            if (_timeSinceLastLevelUp >= levelUpInterval)
            {
                LevelUp();
            }
        }
    }

    private void CalculateDifficulty()
    {
        // The core formula: Difficulty grows over time, then gets multiplied by the stage factor
        float timeFactor = TimeElapsed * timeScalar;
        DifficultyCoefficient = (baseDifficulty + timeFactor) * _currentStageMultiplier;
        
        OnDifficultyChanged?.Invoke(DifficultyCoefficient);
    }

    /// <summary>
    /// Call this method to manually "bump" the difficulty multiplier (e.g., clearing a stage/wave)
    /// </summary>
    public void BumpMultiplier()
    {
        _currentStageMultiplier *= stageMultiplier;
        Debug.Log($"Multiplier Bumped! Current Multiplier: {_currentStageMultiplier:F2}");
        
        // Recalculate immediately so the jump is visible
        CalculateDifficulty();
    }

    public void StartGame()
    {
        TimeElapsed = 0f;
        CurrentLevel = 1;
        _currentStageMultiplier = 1f;
        _timeSinceLastLevelUp = 0f;
        IsGameActive = true;
        
        CalculateDifficulty();
        OnLevelChanged?.Invoke(CurrentLevel);
    }

    public void EndGame()
    {
        IsGameActive = false;
    }

    private void LevelUp()
    {
        CurrentLevel++;
        _timeSinceLastLevelUp = 0f;
        
        // In RoR2, the multiplier bumps when the stage changes.
        // I'm calling it here so your "LevelUpInterval" acts like a stage timer.
        BumpMultiplier(); 
        
        OnLevelChanged?.Invoke(CurrentLevel);
        Debug.Log($"Level Up! Now at level {CurrentLevel}. Difficulty: {DifficultyCoefficient:F2}");
    }
}