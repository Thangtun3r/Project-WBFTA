using UnityEngine;
using _Scripts; 
public class Player : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerStats stats; // Drag your ScriptableObject asset here

    [Header("Sub-Systems")]
    private Movement _movement;
    private PlayerAttack _attack;
    private PlayerVisual _visual;
    private PlayerHealth _health;

    private void Awake()
    {
        _movement = GetComponent<Movement>();
        _attack = GetComponent<PlayerAttack>();
        _visual = GetComponent<PlayerVisual>();
        _health = GetComponent<PlayerHealth>();

        // Pass the data from the SO to the workers
        InitializeWorkerScripts();
    }

    private void InitializeWorkerScripts()
    {
        if (stats == null) return;

        // You would add public "Setup" or "Initialize" methods to your worker scripts
       // _movement.setSpeed(stats.moveSpeed);
        if (_attack != null) _attack.SetDamage(stats.damage);
        if (_health != null) _health.Initialize(stats.maxHealth);
    }
}