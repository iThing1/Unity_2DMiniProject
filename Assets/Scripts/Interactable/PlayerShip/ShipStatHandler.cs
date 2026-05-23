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
    public float BoostAcceleration { get; private set; }
    public float MaxFuel { get; private set; }

    // 상수 스탯
    public float Acceleration { get; private set; }
    public float FuelConsumeRate { get; private set; }
    public float FuelRegenRate { get; private set; }
    public float OverheatDuration { get; private set; }
    public float DockingSpeedThreshold { get; private set; }

    private float _baseBoostAcceleration;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnShipStatsChanged += HandleShipStatsChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnShipStatsChanged -= HandleShipStatsChanged;
    }

    // =========================================================================
    // 스탯 로드
    // =========================================================================
    public void LoadConstantStats()
    {
        var c = GameDataManager.Instance.Constants;

        FuelConsumeRate = c.FuelConsumeRate != 0f ? c.FuelConsumeRate : _defaultFuelConsumeRate;
        FuelRegenRate = c.FuelRegenRate != 0f ? c.FuelRegenRate : _defaultFuelRegenRate;
        OverheatDuration = c.OverheatDuration != 0f ? c.OverheatDuration : _defaultOverheatDuration;
        DockingSpeedThreshold = c.DockingSpeed != 0f ? c.DockingSpeed : _defaultDockingSpeedThreshold;
        Acceleration = c.ShipAcceleration != 0f ? c.ShipAcceleration : _defaultAcceleration;
        _baseBoostAcceleration = c.ShipBoostAccel != 0f ? c.ShipBoostAccel : _defaultBoostAcceleration;
    }

    public void ApplyUpgradeStats(ShipStats stats)
    {
        BaseSpeed = stats.BaseSpeed > 0f ? stats.BaseSpeed : _defaultBaseSpeed;

        BoostAcceleration = stats.BoostAcceleration > 0f
            ? _baseBoostAcceleration * stats.BoostAcceleration
            : _baseBoostAcceleration;

        MaxFuel = stats.MaxFuel > 0f ? stats.MaxFuel : _defaultMaxFuel;
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================

    private void HandleDataInitialized()
    {
        LoadConstantStats();
        ApplyUpgradeStats(GameManager.Instance.SetShipStats());
    }

    private void HandleShipStatsChanged(ShipStats stats)
    {
        ApplyUpgradeStats(stats);
    }
}