using UnityEngine;
using System.Collections.Generic;

public class VFXStation : MonoBehaviour
{
    public static VFXStation Instance { get; private set; }

    [System.Serializable]
    public class VFXEntry
    {
        public string name;
        public ParticleSystem prefab;
    }

    [SerializeField] private VFXEntry[] vfxPrefabs;
    [SerializeField] private int poolSizePerEffect = 20;
    
    private Dictionary<string, ParticleSystem> vfxDictionary;
    private Dictionary<string, Queue<ParticleSystem>> vfxPools;
    private Transform poolParent;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildDictionary();
        InitializePools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildDictionary()
    {
        vfxDictionary = new Dictionary<string, ParticleSystem>();
        foreach (var entry in vfxPrefabs)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning($"VFXStation: Entry '{entry.name}' has null prefab!");
                continue;
            }
            vfxDictionary[entry.name] = entry.prefab;
        }
    }

    private void InitializePools()
    {
        vfxPools = new Dictionary<string, Queue<ParticleSystem>>();
        
        // Create a parent transform for all pooled particles
        GameObject poolParentObj = new GameObject("VFX_PoolParent");
        poolParentObj.transform.SetParent(transform);
        poolParent = poolParentObj.transform;

        // Pre-instantiate particles for each effect
        foreach (var entry in vfxPrefabs)
        {
            if (!vfxDictionary.ContainsKey(entry.name)) continue;

            Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
            
            for (int i = 0; i < poolSizePerEffect; i++)
            {
                ParticleSystem instance = Instantiate(entry.prefab, poolParent);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }
            
            vfxPools[entry.name] = pool;
        }
    }

    /// <summary>
    /// Play a particle effect by name at the specified position
    /// </summary>
    public static void PlayEffect(string effectName, Vector3 position)
    {
        if (Instance == null)
        {
            Debug.LogError("VFXStation: Instance not found!");
            return;
        }

        Instance.PlayEffectInternal(effectName, position, Quaternion.identity);
    }

    /// <summary>
    /// Play a particle effect by name at the specified position with rotation
    /// </summary>
    public static void PlayEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        if (Instance == null)
        {
            Debug.LogError("VFXStation: Instance not found!");
            return;
        }

        Instance.PlayEffectInternal(effectName, position, rotation);
    }

    private void PlayEffectInternal(string effectName, Vector3 position, Quaternion rotation)
    {
        if (!vfxPools.TryGetValue(effectName, out var pool))
        {
            Debug.LogWarning($"VFXStation: Effect '{effectName}' pool not found!");
            return;
        }

        // Get from pool or create new if pool is empty
        ParticleSystem instance;
        if (pool.Count > 0)
        {
            instance = pool.Dequeue();
        }
        else
        {
            if (!vfxDictionary.TryGetValue(effectName, out var prefab))
            {
                Debug.LogWarning($"VFXStation: Effect '{effectName}' prefab not found!");
                return;
            }
            instance = Instantiate(prefab, poolParent);
        }

        // Configure and play
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.gameObject.SetActive(true);
        instance.Play();

        // Return to pool after particle finishes
        var mainModule = instance.main;
        StartCoroutine(ReturnToPoolAfterDelay(effectName, instance, mainModule.duration + 0.1f));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDelay(string effectName, ParticleSystem instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (instance != null)
        {
            instance.gameObject.SetActive(false);
            if (vfxPools.TryGetValue(effectName, out var pool))
            {
                pool.Enqueue(instance);
            }
        }
    }
}

