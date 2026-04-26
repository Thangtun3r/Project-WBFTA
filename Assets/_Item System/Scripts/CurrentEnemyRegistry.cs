using UnityEngine;
using System.Collections.Generic;
using _Scripts.Enemy;

public class CurrentEnemyRegistry : MonoBehaviour
{
    public static CurrentEnemyRegistry Instance { get; private set; }

    private readonly HashSet<BaseEnemy> _activeEnemies = new HashSet<BaseEnemy>();
    public IEnumerable<BaseEnemy> ActiveEnemies => _activeEnemies;

    private void Awake() => Instance = this;

    public void Register(BaseEnemy enemy) => _activeEnemies.Add(enemy);
    public void Unregister(BaseEnemy enemy) => _activeEnemies.Remove(enemy);


    public List<BaseEnemy> GetTargetsInRadius(Vector3 origin, float radius, int maxTargets)
    {
        List<BaseEnemy> results = new List<BaseEnemy>();
        float sqrRad = radius * radius;

        foreach (var enemy in _activeEnemies)
        {
            if ((enemy.transform.position - origin).sqrMagnitude <= sqrRad)
            {
                results.Add(enemy);
                if (results.Count >= maxTargets) break; // Stop once we hit the limit
            }
        }
        return results;
    }
}