using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State (For HUD)")]
    public float TimeElapsed { get; private set; }
    public int CurrentLevel { get; private set; } // Keeps the 120s Stage count for HUD
    public bool IsGameActive { get; private set; }
    
    // The "Master Volume" for your spreadsheet math
    public float DifficultyCoefficient { get; private set; }

    [Header("Spreadsheet Variables")]
    [SerializeField] private float timeFactor = 0.05f; 
    [SerializeField] private float stageFactor = 1.15f; 

    [Header("Pacing")]
    [SerializeField] private float levelUpInterval = 120f;

    public static event Action<float> OnTimeUpdated;
    public static event Action<int> OnLevelChanged;
    public static event Action<float> OnDifficultyChanged;

    private float _timeSinceLastLevelUp;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (IsGameActive)
        {
            TimeElapsed += Time.deltaTime;
            _timeSinceLastLevelUp += Time.deltaTime;
            
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
        float timeInMinutes = TimeElapsed / 60f;
        
        // Matches Spreadsheet: C = (1 + (Min * TimeFactor)) * (StageFactor ^ (Stage - 1))
        DifficultyCoefficient = (1f + (timeInMinutes * timeFactor)) * Mathf.Pow(stageFactor, CurrentLevel - 1);
        
        OnDifficultyChanged?.Invoke(DifficultyCoefficient);
    }

    /// <summary>
    /// USE THIS FOR ENEMIES. 
    /// This level accelerates exponentially even if the HUD Stage is 1.
    /// </summary>
    public int GetAcceleratedLevel()
    {
        // We use the Difficulty Coefficient as the base for the level.
        // As C grows exponentially, this integer jumps faster in the late game.
        return Mathf.Max(1, Mathf.FloorToInt(DifficultyCoefficient));
    }

    public void StartGame()
    {
        TimeElapsed = 0f;
        CurrentLevel = 1;
        _timeSinceLastLevelUp = 0f;
        IsGameActive = true;
        CalculateDifficulty();
        OnLevelChanged?.Invoke(CurrentLevel);
    }

    private void LevelUp()
    {
        CurrentLevel++; // HUD will show Stage 2, 3, 4...
        _timeSinceLastLevelUp = 0f;
        CalculateDifficulty();
        OnLevelChanged?.Invoke(CurrentLevel);
    }
}