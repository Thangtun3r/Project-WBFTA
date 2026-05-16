using System;
using UnityEngine;

public class PiercingSpikeLauncher : MonoBehaviour, IProjectile
{
    [Header("Spike Settings")]
    [SerializeField] private string spikeProjectileId = "PiercingSpike";
    [SerializeField] private float spikeSpeed = 10f;
    [SerializeField] private float spikeLifetime = 1.5f;
    [SerializeField] private float randomAngleOffset = 15f;

    private Action<IProjectile> _onRelease;
    private static readonly Vector2[] BaseDirections =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    public void Launch(ProjectileRequest request, Action<IProjectile> onRelease)
    {
        _onRelease = onRelease;

        for (int i = 0; i < BaseDirections.Length; i++)
        {
            SpawnSpike(request, ApplyRandomOffset(BaseDirections[i]));
        }

        Deactivate();
    }

    private Vector2 ApplyRandomOffset(Vector2 baseDirection)
    {
        float offset = UnityEngine.Random.Range(-randomAngleOffset, randomAngleOffset);
        return Quaternion.Euler(0f, 0f, offset) * baseDirection;
    }

    private void SpawnSpike(ProjectileRequest sourceRequest, Vector2 direction)
    {
        float effectiveSpikeSpeed = sourceRequest.Speed > 0f ? sourceRequest.Speed : spikeSpeed;
        float effectiveSpikeLifetime = sourceRequest.Lifetime > 0f ? sourceRequest.Lifetime : spikeLifetime;

        ProjectileRequest spikeRequest = new ProjectileRequest
        {
            ProjectileID = spikeProjectileId,
            Position = sourceRequest.Position,
            Rotation = Quaternion.FromToRotation(Vector3.right, direction),
            Direction = (Vector3)(direction.normalized * effectiveSpikeSpeed),
            Target = null,
            Damage = sourceRequest.Damage,
            Speed = effectiveSpikeSpeed,
            Lifetime = effectiveSpikeLifetime
        };

        ProjectilePool.Instance?.RequestProjectile(spikeRequest);
    }

    private void Deactivate()
    {
        _onRelease?.Invoke(this);
    }
}
