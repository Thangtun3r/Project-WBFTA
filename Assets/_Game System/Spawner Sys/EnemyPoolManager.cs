using System.Collections.Generic;
using UnityEngine;

public class EnemyPoolManager : MonoBehaviour
{
    public static EnemyPoolManager Instance;
    private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake() => Instance = this;

    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();

        GameObject obj;
        if (_pools[prefab].Count > 0)
        {
            obj = _pools[prefab].Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, transform);
        }

        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();
        _pools[prefab].Enqueue(instance);
    }
}