//This is the manager that will listen to events like onhit, onpickup, etc. 
//and then call the appropriate item logic methods.

using UnityEngine;
using System;
using _Scripts.Enemy;

public class GlobalEventManager : MonoBehaviour
{
    public static GlobalEventManager Instance { get; private set; }
    public event Action<GameObject, IDamagable, float, bool, float> HandleOnHit;
    public event Action<BaseEnemy, float, bool> OnEnemyKilledWithStats;

    public PlayerStatMachine PlayerStats { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to enemy kill events
        _Scripts.Enemy.BaseEnemy.OnEnemyKilled += OnEnemyKilledHandler;
    }

    private void OnDestroy()
    {
        _Scripts.Enemy.BaseEnemy.OnEnemyKilled -= OnEnemyKilledHandler;
    }

    public void OnHit(GameObject attacker, IDamagable target, float damage, bool isCrit, float procCoefficient = 1f)
    {
        HandleOnHit?.Invoke(attacker, target, damage, isCrit, Mathf.Max(0f, procCoefficient));
    }

    // Centralized method for any player-owned tool/weapon to trigger items
    // This automatically associates the damage with the Player to trigger their items securely
    public void OnPlayerHit(IDamagable target, float damage, bool isCrit, float procCoefficient = 1f)
    {
        // Find whoever actually holds the PlayerInventory, since ItemRuntime registers its OwnerObject as the PlayerInventory's gameObject
        GameObject ownerObj = null;
        var inventory = FindAnyObjectByType<PlayerInventory>();

        if (inventory != null)
        {
            ownerObj = inventory.gameObject;
        }
        else
        {
            var playerScript = FindAnyObjectByType<Player>();
            if (playerScript != null)
            {
                ownerObj = playerScript.gameObject;
            }
            else
            {
                return;
            }
        }

        HandleOnHit?.Invoke(ownerObj, target, damage, isCrit, Mathf.Max(0f, procCoefficient));
    }

    // Handles enemy kill and retrieves current player damage/crit stats
    private void OnEnemyKilledHandler(_Scripts.Enemy.BaseEnemy enemy)
    {
        Debug.Log($"[GlobalEventManager] Enemy killed: {enemy.name}");

        if (PlayerStats == null)
        {
            Debug.LogWarning("[GlobalEventManager] PlayerStats is NULL! OnEnemyKilledWithStats won't be invoked.");
            return;
        }

        if (enemy == null)
        {
            Debug.LogWarning("[GlobalEventManager] Enemy is NULL!");
            return;
        }

        // Get the current player's damage and crit status
        float baseDamage = PlayerStats.GetCalculatedAttackDamage();
        bool isCrit = PlayerStats.WasLastAttackCrit();

        Debug.Log($"[GlobalEventManager] Invoking OnEnemyKilledWithStats with damage={baseDamage}, isCrit={isCrit}");

        // Broadcast kill with damage and crit info to all subscribers
        OnEnemyKilledWithStats?.Invoke(enemy, baseDamage, isCrit);
    }
}
