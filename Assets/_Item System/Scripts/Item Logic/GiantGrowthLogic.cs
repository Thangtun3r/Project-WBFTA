using UnityEngine;

public class GiantGrowthLogic : ItemLogicBase, IPlayerStatProvider
{
    private const string SizeMultiplierPerStackKey = "GiantGrowth.SizeMultiplierPerStack";
    private const string DamageMultiplierPerStackKey = "GiantGrowth.DamageMultiplierPerStack";
    private const float FallbackSizeMultiplierPerStack = 0.1f;
    private const float FallbackDamageMultiplierPerStack = 0.1f;

    private Transform _ownerTransform;
    private Vector3 _baseScale;

    protected override void OnInitialize()
    {
        _ownerTransform = Owner.OwnerObject != null ? Owner.OwnerObject.transform : null;
        if (_ownerTransform == null)
        {
            return;
        }

        _baseScale = _ownerTransform.localScale;
        ApplyScale();
    }

    protected override void HandleStackChanged(int amountChanged)
    {
        ApplyScale();
    }

    public override void Dispose()
    {
        if (_ownerTransform != null)
        {
            _ownerTransform.localScale = _baseScale;
        }
    }

    public void ModifyPlayerStat(ref PlayerStatQuery query)
    {
        if (query.StatType != PlayerStatType.AttackDamage)
        {
            return;
        }

        query.Multiplier *= GetStackMultiplier(DamageMultiplierPerStackKey, FallbackDamageMultiplierPerStack);
    }

    private void ApplyScale()
    {
        if (_ownerTransform == null)
        {
            return;
        }

        _ownerTransform.localScale = _baseScale * GetStackMultiplier(SizeMultiplierPerStackKey, FallbackSizeMultiplierPerStack);
    }

    private float GetStackMultiplier(string parameterKey, float fallbackPerStack)
    {
        float perStack = Owner.GetParameter(parameterKey, fallbackPerStack);
        return Mathf.Max(0f, 1f + Mathf.Max(Owner.StackSize, 0) * perStack);
    }
}
