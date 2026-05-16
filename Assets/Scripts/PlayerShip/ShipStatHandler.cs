using UnityEngine;
using GameData;

[RequireComponent(typeof(Rigidbody2D))]
public class ShipStatHandler : MonoBehaviour
{
    // =========================================================================
    // Inspector 기본값
    // =========================================================================
    [Header("이동 스탯 기본값")]
    [SerializeField] private float _defaultBaseSpeed = 10f;
    [SerializeField] private float _defaultAcceleration = 5f;
    [SerializeField] private float _defaultBoostAcceleration = 20f;

    [Header("연료 스탯 기본값")]
    [SerializeField] private float _defaultMaxFuel = 200f;
    [SerializeField] private float _defaultFuelConsumeRate = 50f;
    [SerializeField] private float _defaultFuelRegenRate = 20f;
    [SerializeField] private float _defaultOverheatDuration = 5f;
    [SerializeField] private float _defaultDockingSpeedThreshold = 0.2f;

    // =========================================================================
    // 스탯 프로퍼티
    // =========================================================================

    // 업그레이드 스탯
    public float BaseSpeed { get; private set; }
    public float Acceleration { get; private set; }
    public float BoostAcceleration { get; private set; }
    public float MaxFuel { get; private set; }

    // 상수 스탯
    public float FuelConsumeRate { get; private set; }
    public float FuelRegenRate { get; private set; }
    public float OverheatDuration { get; private set; }
    public float DockingSpeedThreshold { get; private set; }

    // =========================================================================
    // Upgrade / Constant ID 상수
    // =========================================================================
    private const string UPGRADE_SPEED = "UP_Ship_Speed";
    private const string UPGRADE_ACCEL = "UP_Ship_Accel";
    private const string UPGRADE_MAX_FUEL = "UP_Ship_MaxFuel";

    private const string CONST_FUEL_CONSUME = "FUEL_CONSUME_RATE";
    private const string CONST_FUEL_REGEN = "FUEL_REGEN_RATE";
    private const string CONST_OVERHEAT_DUR = "OVERHEAT_DURATION";
    private const string CONST_DOCKING_SPEED = "STATION_DOCKING_SPEED";
    private const string CONST_ACCELERATION = "SHIP_ACCELERATION";
    private const string CONST_BOOST_ACCEL = "SHIP_BOOST_ACCELERATION";

    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnUpgradeCompleted += HandleUpgradeCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnUpgradeCompleted -= HandleUpgradeCompleted;
    }

    // =========================================================================
    // 스탯 로드
    // =========================================================================
    private void LoadConstantStats()
    {
        var dm = GameDataManager.Instance;

        FuelConsumeRate = dm.Get<GameConstantData>(CONST_FUEL_CONSUME)?.Value ?? _defaultFuelConsumeRate;
        FuelRegenRate = dm.Get<GameConstantData>(CONST_FUEL_REGEN)?.Value ?? _defaultFuelRegenRate;
        OverheatDuration = dm.Get<GameConstantData>(CONST_OVERHEAT_DUR)?.Value ?? _defaultOverheatDuration;
        DockingSpeedThreshold = dm.Get<GameConstantData>(CONST_DOCKING_SPEED)?.Value ?? _defaultDockingSpeedThreshold;
        Acceleration = dm.Get<GameConstantData>(CONST_ACCELERATION)?.Value ?? _defaultAcceleration;
        BoostAcceleration = dm.Get<GameConstantData>(CONST_BOOST_ACCEL)?.Value ?? _defaultBoostAcceleration;
    }

    public void RefreshUpgradeStats()
    {
        var gm = GameManager.Instance;

        float speedStat = gm.GetUpgradeStat(UPGRADE_SPEED);
        BaseSpeed = speedStat > 0f ? speedStat : _defaultBaseSpeed;

        float accelMult = gm.GetUpgradeStat(UPGRADE_ACCEL);
        float baseBoost = GameDataManager.Instance.Get<GameConstantData>(CONST_BOOST_ACCEL)?.Value
                          ?? _defaultBoostAcceleration;
        BoostAcceleration = accelMult > 0f ? baseBoost * accelMult : baseBoost;

        float fuelStat = gm.GetUpgradeStat(UPGRADE_MAX_FUEL);
        MaxFuel = fuelStat > 0f ? fuelStat : _defaultMaxFuel;
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================

    private void HandleDataInitialized()
    {
        LoadConstantStats();
        RefreshUpgradeStats();
    }

    private void HandleUpgradeCompleted(string upgradeId, int newLevel)
    {
        switch (upgradeId)
        {
            case UPGRADE_SPEED:
            case UPGRADE_ACCEL:
            case UPGRADE_MAX_FUEL:
                RefreshUpgradeStats();
                break;
        }
    }
}