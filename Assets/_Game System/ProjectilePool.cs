using UnityEngine;
using UnityEngine.Pool;
using System;

// This lives in the same file or a GlobalTypes.cs
public struct ProjectileRequest
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float Damage;
    public Vector2 Velocity;

    public ProjectileRequest(Vector3 pos, Quaternion rot, float dmg, Vector2 vel)
    {
        Position = pos;
        Rotation = rot;
        Damage = dmg;
        Velocity = vel;
    }
}

public class ProjectilePool : MonoBehaviour
{
    public static Action<ProjectileRequest> OnProjectileRequested;

    [SerializeField] private Projectile prefab; // Drag your Projectile Prefab here
    private ObjectPool<Projectile> _pool;

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError("ProjectilePool: No Prefab assigned! Drag the Projectile prefab into the inspector.");
            return;
        }

        _pool = new ObjectPool<Projectile>(
            createFunc: () => Instantiate(prefab), // Instantiating the script type returns the script reference!
            actionOnGet: (p) => p.gameObject.SetActive(true),
            actionOnRelease: (p) => p.gameObject.SetActive(false),
            actionOnDestroy: (p) => Destroy(p.gameObject),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    private void OnEnable() => OnProjectileRequested += HandleProjectileRequest;
    private void OnDisable() => OnProjectileRequested -= HandleProjectileRequest;

    private void HandleProjectileRequest(ProjectileRequest request)
    {
        if (_pool == null) return;

        Projectile p = _pool.Get();
        
        // Use SetPositionAndRotation for a tiny bit more performance than setting them separately
        p.transform.SetPositionAndRotation(request.Position, request.Rotation);
        
        // Pass the damage and velocity, and give the projectile a way to "come back home" to the pool
        p.Launch(request.Damage, request.Velocity, ReleaseProjectile);
    }

    private void ReleaseProjectile(Projectile p)
    {
        _pool.Release(p);
    }
}