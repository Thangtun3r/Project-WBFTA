using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    public float TimeElapsed { get; private set; }
    public int CurrentLevel { get; private set; }
    public bool IsGameActive { get; private set; }
    
    // This is the "C" value from your spreadsheet
    public float DifficultyCoefficient { get; private set; }

    [Header("Spreadsheet Variables")]
    [Tooltip("Matches J7 in your sheet (e.g., 0.05)")]
    [SerializeField] private float timeFactor = 0.05f; 
    
    [Tooltip("Matches J10 in your sheet (e.g., 1.15)")]
    [SerializeField] private float stageFactor = 1.15f; 

    [Header("Pacing")]
    [Tooltip("How long one stage lasts before jumping difficulty (e.g., 120s = 2 mins)")]
    [SerializeField] private float levelUpInterval = 120f;

    public static event Action<float> OnTimeUpdated;
    public static event Action<int> OnLevelChanged;
    public static event Action<float> OnDifficultyChanged;

    private float _timeSinceLastLevelUp;

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
        // 1. Convert total seconds to minutes to match spreadsheet logic
        float timeInMinutes = TimeElapsed / 60f;
        
        // 2. The Formula: C = (1 + (Minutes * TimeFactor)) * (StageFactor ^ (Stage - 1))
        // This ensures Stage 1 starts with a multiplier of 1.0 (StageFactor^0)
        float timePart = 1f + (timeInMinutes * timeFactor);
        float stagePart = Mathf.Pow(stageFactor, CurrentLevel - 1);
        
        DifficultyCoefficient = timePart * stagePart;
        
        OnDifficultyChanged?.Invoke(DifficultyCoefficient);
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

    public void EndGame()
    {
        IsGameActive = false;
    }

    private void LevelUp()
    {
        CurrentLevel++;
        _timeSinceLastLevelUp = 0f;
        
        // Recalculate immediately to apply the StageFactor "Jump"
        CalculateDifficulty();
        
        OnLevelChanged?.Invoke(CurrentLevel);
        Debug.Log($"STAGE JUMP! Now at Level {CurrentLevel}. Difficulty Coefficient: {DifficultyCoefficient:F2}");
    }

    // Optional: Call this if you want to force a stage jump (e.g., killing a boss early)
    public void ForceLevelUp()
    {
        LevelUp();
    }
}