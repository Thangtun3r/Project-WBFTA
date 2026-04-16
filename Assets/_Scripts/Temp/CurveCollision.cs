using UnityEngine;
using System.Collections.Generic;

public class CurveDamageProcessor : MonoBehaviour
{
    [Header("References")]
    public WizardDesignerPen penTool;
    public LineRenderer lineRenderer;
    public FloatingDamagePool damagePool; // THE DRAG-AND-DROP SLOT

    [Header("2D Combat Settings")]
    public float damagePerTick = 2f;
    public float hitRadius = 0.4f;
    public LayerMask enemyLayer;
    public float damageInterval = 0.1f; 

    private Dictionary<IDamagable, float> lastHitTimes = new Dictionary<IDamagable, float>();

    void Update()
    {
        if (lineRenderer != null && lineRenderer.positionCount > 1)
        {
            ProcessAlwaysDamage();
        }
    }

    void ProcessAlwaysDamage()
    {
        int pointCount = lineRenderer.positionCount;
        Vector3[] curvePoints = new Vector3[pointCount];
        lineRenderer.GetPositions(curvePoints);

        for (int i = 0; i < curvePoints.Length; i += 3)
        {
            Vector2 checkPos = new Vector2(curvePoints[i].x, curvePoints[i].y);
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(checkPos, hitRadius, enemyLayer);

            foreach (var col in hitEnemies)
            {
                if (col.TryGetComponent(out IDamagable target))
                {
                    if (!lastHitTimes.ContainsKey(target) || Time.time >= lastHitTimes[target] + damageInterval)
                    {
                        target.TakeDamage(damagePerTick);
                        lastHitTimes[target] = Time.time;

                        // --- DIRECT CALL ---
                        if (damagePool != null)
                        {
                            damagePool.SpawnDamage(checkPos, damagePerTick, false);
                        }
                    }
                }
            }
        }
    }
}