using UnityEngine;

public interface IPlayerWeapon
{
    float DamageMultiplier { get; }
    float ProcCoefficient { get; }
    Sprite CurrentSprite { get; }
    void SetWeaponActive(bool active);
}

public interface IPlayerWeaponIconRotation
{
    bool TryGetIconRotation(out Quaternion rotation);
}
