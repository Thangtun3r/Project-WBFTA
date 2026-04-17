using UnityEngine;
using _Scripts; 
public class Player : MonoBehaviour
{
    [Header("Sub-Systems")]
    private Movement _movement;
    private PlayerAttack _attack;
    private PlayerVisual _visual;
    private PlayerHealth _health;
    private PlayerStatMachine _statMachine;

    private void Awake()
    {
        _movement = GetComponent<Movement>();
        _attack = GetComponent<PlayerAttack>();
        _visual = GetComponent<PlayerVisual>();
        _health = GetComponent<PlayerHealth>();
        _statMachine = GetComponent<PlayerStatMachine>();

      
    }

    void Start()
    {
        InitializeWorkerScripts();
    }

    private void InitializeWorkerScripts()
    {
        if (_statMachine == null) return;

        // Use stat machine's base values to initialize subsystems
        if (_attack != null) _attack.SetDamage(_statMachine.GetBaseDamage());
        if (_health != null) _health.Initialize(_statMachine.GetBaseHealth());
    }
}