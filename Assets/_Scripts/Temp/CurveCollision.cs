using UnityEngine;
using System.Collections.Generic;

public class CurveDamageProcessor : MonoBehaviour, IPlayerWeapon
{
    [Header("References")]
    public WizardDesignerPen penTool;
    public LineRenderer lineRenderer;
    [SerializeField] private PlayerAttack playerAttack;

    [Header("2D Combat Settings")]
    public float hitRadius = 0.4f;
    public LayerMask enemyLayer;
    public float damageInterval = 0.1f; 

    [Header("Weapon Tuning")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float procCoefficient = 1f;

    [Header("Icon")]
    [SerializeField] private Sprite iconSprite;

    private Dictionary<IDamagable, float> lastHitTimes = new Dictionary<IDamagable, float>();
    private bool _active = true;

    public float DamageMultiplier => damageMultiplier;
    public float ProcCoefficient => procCoefficient;
    public Sprite CurrentSprite => iconSprite;

    private void Awake()
    {
        if (playerAttack == null)
            playerAttack = FindFirstObjectByType<PlayerAttack>();
    }

    void Update()
    {
        if (_active && lineRenderer != null && lineRenderer.positionCount > 1)
        {
            ProcessAlwaysDamage();
        }
    }

    public void SetWeaponActive(bool active)
    {
        _active = active;
        enabled = active;

        if (penTool != null)
        {
            penTool.CancelDrawing();
            penTool.enabled = active;
        }
    }

    void ProcessAlwaysDamage()
    {
        if (playerAttack == null)
            return;

        int pointCount = lineRenderer.positionCount;
        Vector3[] curvePoints = new Vector3[pointCount];
        lineRenderer.GetPositions(curvePoints);

        for (int i = 0; i < curvePoints.Length; i += 3)
        {
            Vector2 checkPos = new Vector2(curvePoints[i].x, curvePoints[i].y);
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(checkPos, hitRadius, enemyLayer);

            foreach (var col in hitEnemies)
            {
                IDamagable target = col.GetComponent<IDamagable>() ?? col.GetComponentInParent<IDamagable>();
                if (target != null)
                {
                    if (!lastHitTimes.ContainsKey(target) || Time.time >= lastHitTimes[target] + damageInterval)
                    {
                        playerAttack.DealDamage(target, checkPos, damageMultiplier, procCoefficient, gameObject);
                        lastHitTimes[target] = Time.time;
                    }
                }
            }
        }
    }
}
