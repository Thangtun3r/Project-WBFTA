using UnityEngine;

public class SniperRicochetSkill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SniperCursorWeapon sniperWeapon;

    [Header("Input")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q;

    [Header("Ricochet")]
    [SerializeField] private int bounces = 2;
    [SerializeField] private float activeDuration = 6f;
    [SerializeField] private float cooldown = 1.5f;

    private bool _skillActive = true;
    private float _nextReadyTime;
    private float _activeUntilTime = -Mathf.Infinity;

    private void Awake()
    {
        if (sniperWeapon == null)
            sniperWeapon = GetComponent<SniperCursorWeapon>();
    }

    private void Update()
    {
        if (!_skillActive || sniperWeapon == null)
            return;
        if (Time.time < _nextReadyTime)
            return;

        if (Input.GetKeyDown(triggerKey))
        {
            _activeUntilTime = Time.time + Mathf.Max(0f, activeDuration);
            _nextReadyTime = _activeUntilTime + Mathf.Max(0f, cooldown);
        }
    }

    public bool TryGetActiveRicochetBounces(out int activeBounces)
    {
        activeBounces = 0;
        if (!_skillActive || Time.time > _activeUntilTime)
            return false;

        activeBounces = Mathf.Max(0, bounces);
        return activeBounces > 0;
    }

    public void SetSkillActive(bool active)
    {
        _skillActive = active;
        if (!_skillActive)
            _activeUntilTime = -Mathf.Infinity;
    }
}
