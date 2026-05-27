using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class WorldObjectSpawner : MonoBehaviour
{
    public static WorldObjectSpawner Instance { get; private set; }

    [System.Serializable]
    public class SpawnEntry
    {
        public string key; 
        public GameObject prefab;
    }

    public List<SpawnEntry> library = new List<SpawnEntry>();
    private Dictionary<string, GameObject> _lookup;

    private void Awake()
    {
        Instance = this;
        _lookup = new Dictionary<string, GameObject>();
        foreach (var entry in library)
        {
            if (!string.IsNullOrEmpty(entry.key))
                _lookup[entry.key] = entry.prefab;
        }
    }

    public GameObject Spawn(string key, Vector3 position, object data = null)
    {
        if (_lookup.TryGetValue(key, out GameObject prefab))
        {
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            
    
            if (instance.TryGetComponent<IWorldObjectSpawner>(out var spawnable))
            {
                spawnable.OnSpawn(data);
            }
            
            return instance;
        }
        return null;
    }
}