using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner2D : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float despawnTime = 5f;

    [Header("Spawn Logic")]
    [SerializeField] private Collider2D spawnArea; // Use Collider2D (Box, Polygon, or Composite)
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 2f;
    [SerializeField] private int maxActiveEnemies = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private float timer = 0f;
    private float currentSpawnInterval;

    private int ActiveEnemyCount => poolSize - pool.Count;

    void Start()
    {
        InitializePool();
        if (maxActiveEnemies <= 0 || maxActiveEnemies > poolSize)
        {
            maxActiveEnemies = poolSize;
        }

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

    private Vector2 GetPointWithinCameraViewport()
    {
        Camera cam = Camera.main;
        if (cam == null) return (Vector2)transform.position;

        float zDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 viewportPoint = new Vector3(Random.value, Random.value, zDistance);
        Vector3 worldPoint = cam.ViewportToWorldPoint(viewportPoint);
        Vector2 spawnPos = new Vector2(worldPoint.x, worldPoint.y);

        if (spawnArea != null)
        {
            spawnPos = spawnArea.ClosestPoint(spawnPos);
        }

        return spawnPos;
    }

    public GameObject Spawn(Vector2 position, Quaternion rotation)
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
        return null;
    }

    private IEnumerator Despawn(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        ReturnToPool(obj);
    }

    public void ReturnToPool(GameObject obj)
    {
        if (!obj.activeInHierarchy) return;
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= currentSpawnInterval)
        {
            if (ActiveEnemyCount < maxActiveEnemies)
            {
                Vector2 spawnPos = GetPointWithinCameraViewport();
                Spawn(spawnPos, Quaternion.identity);

                timer = 0f;
                currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            }
        }
    }
}