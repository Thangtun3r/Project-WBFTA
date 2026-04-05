using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 2f;
    [SerializeField] private float despawnTime = 5f;
    private Queue<GameObject> pool = new Queue<GameObject>();
    private float timer = 0f;
    private float currentSpawnInterval;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializePool();
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            StartCoroutine(Despawn(obj, despawnTime));
            return obj;
        }
        else
        {
            Debug.LogWarning("Pool is empty!");
            return null;
        }
    }

    private IEnumerator Despawn(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        ReturnToPool(obj);
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= currentSpawnInterval)
        {
            Spawn(transform.position, Quaternion.identity);
            timer = 0f;
            currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }
}
