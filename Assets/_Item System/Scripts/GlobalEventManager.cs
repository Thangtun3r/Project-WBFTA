//This is the manager that will listen to events like onhit, onpickup, etc. 
//and then call the appropriate item logic methods.

using UnityEngine;
using System;

public class GlobalEventManager : MonoBehaviour
{
    public static GlobalEventManager Instance { get; private set; }
    public event Action<IDamagable, float, bool> HandleOnHit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnHit(IDamagable target, float damage, bool isCrit)
    {
        HandleOnHit?.Invoke(target, damage, isCrit);
    }




}