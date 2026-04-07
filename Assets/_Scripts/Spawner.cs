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
    [SerializeField] private float playerSpawnRadius = 15f; 
    [SerializeField] private float minPlayerDistance = 4f; 
    [SerializeField] private int initialPreSpawnCount = 8;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private Transform playerTransform;
    private float timer = 0f;
    private float currentSpawnInterval;

    void Start()
    {
        InitializePool();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        PreSpawnEnemies();
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

    private void PreSpawnEnemies()
    {
        for (int i = 0; i < initialPreSpawnCount; i++)
        {
            Vector2 randomPoint = GetRandomPointInCollider2D(spawnArea);
            Spawn(randomPoint, Quaternion.identity);
        }
    }

    private Vector2 GetRandomPointInCollider2D(Collider2D col)
    {
        Bounds bounds = col.bounds;
        Vector2 point = new Vector2(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y)
        );

        // ClosestPoint ensures that even if our random box-math 
        // lands outside a complex shape, it snaps to the edge.
        return col.ClosestPoint(point);
    }

    private Vector2 GetPointNearPlayer()
    {
        if (playerTransform == null) return (Vector2)transform.position;

        // Generate a random point in a ring around the player
        Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(minPlayerDistance, playerSpawnRadius);
        Vector2 spawnPos = (Vector2)playerTransform.position + randomOffset;

        // Constrain it so enemies don't spawn outside the "Map" Collider
        return spawnArea.ClosestPoint(spawnPos);
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
            Vector2 spawnPos = GetPointNearPlayer();
            Spawn(spawnPos, Quaternion.identity);

            timer = 0f;
            currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }
}