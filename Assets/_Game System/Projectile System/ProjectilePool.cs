using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [Serializable]
    public struct ProjectileEntry {
        public string id;
        public GameObject prefab;
        public int initialCapacity; // Set to 40 in Inspector
        public int maxCapacity;     // Set to 2000+ for crazy stacks
    }

    [SerializeField] private List<ProjectileEntry> projectileLibrary;
    private Dictionary<string, ObjectPool<GameObject>> _pools = new();

    private void Awake() {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var entry in projectileLibrary) {
            // Use local variables to avoid closure issues in createFunc
            GameObject prefabToSpawn = entry.prefab;
            
            _pools[entry.id] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefabToSpawn),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false, // Performance: Set to false if you handle safety in the projectile script
                defaultCapacity: entry.initialCapacity > 0 ? entry.initialCapacity : 40,
                maxSize: entry.maxCapacity > 0 ? entry.maxCapacity : 2000 
            );

            // "Pre-warm" the pool by instantiating the initial capacity immediately
            PreWarmPool(entry.id, entry.initialCapacity > 0 ? entry.initialCapacity : 40);
        }
    }

    private void PreWarmPool(string id, int amount)
    {
        var pool = _pools[id];
        List<GameObject> temp = new List<GameObject>();

        // Get 40 items (triggers createFunc), then release them back to the pool
        for (int i = 0; i < amount; i++) {
            temp.Add(pool.Get());
        }
        foreach (var obj in temp) {
            pool.Release(obj);
        }
    }

    public void RequestProjectile(ProjectileRequest request) {
        if (_pools.TryGetValue(request.ProjectileID, out var pool)) {
            GameObject go = pool.Get();
            go.transform.SetPositionAndRotation(request.Position, request.Rotation);
            
            if (go.TryGetComponent(out IProjectile projectile)) {
                projectile.Launch(request, (p) => pool.Release(go));
            } else {
                Debug.LogWarning($"Prefab {request.ProjectileID} is missing IProjectile script!");
                pool.Release(go);
            }
        }
    }
}