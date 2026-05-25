using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Weapon Modules")]
    [SerializeField] private MonoBehaviour[] weaponModules;

    [Header("Legacy Weapon Slots")]
    [SerializeField] private MonoBehaviour slashWeapon;
    [SerializeField] private MonoBehaviour dragWeapon;
    [SerializeField] private MonoBehaviour penWeapon;
    [SerializeField] private MonoBehaviour sniperWeapon;
    [SerializeField] private int startingWeaponIndex;

    [Header("Weapon Icon")]
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;

    private IPlayerWeapon[] _weapons;
    private int _currentWeaponIndex = -1;
    private Sprite _lastAppliedSprite;
    private Quaternion _defaultWeaponSpriteLocalRotation;

    private void Awake()
    {
        if (weaponSpriteRenderer != null)
            _defaultWeaponSpriteLocalRotation = weaponSpriteRenderer.transform.localRotation;

        ResolveLegacySlots();
        _weapons = ResolveWeaponModules();
    }

    private void Start()
    {
        if (_weapons == null || _weapons.Length == 0)
            return;

        SelectWeapon(Mathf.Clamp(startingWeaponIndex, 0, _weapons.Length - 1));
    }

    private void Update()
    {
        HandleNumberKeySelection();

        UpdateWeaponSprite();
        UpdateWeaponIconRotation();
    }

    public void SelectWeapon(int index)
    {
        if (_weapons == null || index < 0 || index >= _weapons.Length)
            return;

        for (int i = 0; i < _weapons.Length; i++)
        {
            _weapons[i]?.SetWeaponActive(false);
        }

        if (_weapons[index] == null)
        {
            _currentWeaponIndex = -1;
            return;
        }

        _weapons[index].SetWeaponActive(true);
        _currentWeaponIndex = index;
        UpdateWeaponSprite(true);
    }

    public int CurrentWeaponIndex => _currentWeaponIndex;

    private static IPlayerWeapon ResolveWeapon(MonoBehaviour candidate)
    {
        if (candidate == null)
            return null;

        if (candidate is IPlayerWeapon weapon)
            return weapon;

        return candidate.GetComponent<IPlayerWeapon>() ?? candidate.GetComponentInChildren<IPlayerWeapon>();
    }

    private void ResolveLegacySlots()
    {
        if (slashWeapon == null)
            slashWeapon = GetComponentInChildren<CollisionWeapon>();

        if (dragWeapon == null)
            dragWeapon = FindFirstObjectByType<DragableCursor>();

        if (penWeapon == null)
            penWeapon = FindFirstObjectByType<CurveDamageProcessor>();

        if (sniperWeapon == null)
            sniperWeapon = FindFirstObjectByType<SniperCursorWeapon>();
    }

    private IPlayerWeapon[] ResolveWeaponModules()
    {
        MonoBehaviour[] sourceModules = weaponModules != null && weaponModules.Length > 0
            ? weaponModules
            : new[] { slashWeapon, dragWeapon, penWeapon, sniperWeapon };

        IPlayerWeapon[] resolvedWeapons = new IPlayerWeapon[sourceModules.Length];
        for (int i = 0; i < sourceModules.Length; i++)
        {
            resolvedWeapons[i] = ResolveWeapon(sourceModules[i]);
        }

        return resolvedWeapons;
    }

    private void HandleNumberKeySelection()
    {
        if (_weapons == null)
            return;

        int keyCount = Mathf.Min(_weapons.Length, 9);
        for (int i = 0; i < keyCount; i++)
        {
            KeyCode keyCode = (KeyCode)((int)KeyCode.Alpha1 + i);
            if (Input.GetKeyDown(keyCode))
                SelectWeapon(i);
        }
    }

    private void UpdateWeaponSprite(bool force = false)
    {
        if (weaponSpriteRenderer == null || _weapons == null || _currentWeaponIndex < 0 || _currentWeaponIndex >= _weapons.Length)
            return;

        Sprite currentSprite = _weapons[_currentWeaponIndex]?.CurrentSprite;
        if (!force && currentSprite == _lastAppliedSprite)
            return;

        if (currentSprite != null)
        {
            weaponSpriteRenderer.sprite = currentSprite;
            _lastAppliedSprite = currentSprite;
        }
    }

    private void UpdateWeaponIconRotation()
    {
        if (weaponSpriteRenderer == null || _weapons == null || _currentWeaponIndex < 0 || _currentWeaponIndex >= _weapons.Length)
            return;

        if (_weapons[_currentWeaponIndex] is IPlayerWeaponIconRotation rotator
            && rotator.TryGetIconRotation(out Quaternion rotation))
        {
            weaponSpriteRenderer.transform.rotation = rotation;
            return;
        }

        weaponSpriteRenderer.transform.localRotation = _defaultWeaponSpriteLocalRotation;
    }
}
