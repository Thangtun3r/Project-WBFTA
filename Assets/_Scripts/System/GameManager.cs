using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float TimeElapsed { get; private set; }
    public int CurrentLevel { get; private set; }
    public bool IsGameActive { get; private set; }

    [SerializeField] private float levelUpInterval = 30f;

    public static event Action<float> OnTimeUpdated;
    public static event Action<int> OnLevelChanged;

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
        // Start tracking time the moment the game starts
        StartGame();
    }

    private void Update()
    {
        if (IsGameActive)
        {
            TimeElapsed += Time.deltaTime;
            _timeSinceLastLevelUp += Time.deltaTime;
            OnTimeUpdated?.Invoke(TimeElapsed);

            if (_timeSinceLastLevelUp >= levelUpInterval)
            {
                LevelUp();
            }
        }
    }

    public void StartGame()
    {
        TimeElapsed = 0f;
        CurrentLevel = 1;
        _timeSinceLastLevelUp = 0f;
        IsGameActive = true;
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
        OnLevelChanged?.Invoke(CurrentLevel);
        Debug.Log($"Level Up! Now at level {CurrentLevel}");
    }
}
