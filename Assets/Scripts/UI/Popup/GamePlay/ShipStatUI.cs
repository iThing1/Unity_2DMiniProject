using TMPro;
using UnityEngine;

public class ShipStatUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("스탯 목록")]
    [SerializeField] private TMP_Text _txtSpeed;
    [SerializeField] private TMP_Text _txtBoost;
    [SerializeField] private TMP_Text _txtMaxFuel;
    [SerializeField] private TMP_Text _txtLoaderSpeed;
    [SerializeField] private TMP_Text _txtDockingSpeed;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void RegisterEvents()
    {
        GameEventBus.Subscribe<ShipStats>(GameEventType.ShipStatsChanged, HandleShipStatsChanged);
    }

    protected override void UnregisterEvents()
    {
        GameEventBus.Unsubscribe<ShipStats>(GameEventType.ShipStatsChanged, HandleShipStatsChanged);
    }

    // =========================================================================
    // 열기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        RefreshUI(GameManager.Instance.SetShipStats());
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleShipStatsChanged(ShipStats stats)
    {
        RefreshUI(stats);
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshUI(ShipStats stats)
    {
        if (_txtSpeed != null)
            _txtSpeed.text = $"기본 속도  {stats.BaseSpeed:F1}";

        if (_txtBoost != null)
            _txtBoost.text = $"부스트 가속  {stats.BoostAcceleration:F1}";

        if (_txtMaxFuel != null)
            _txtMaxFuel.text = $"최대 연료  {stats.MaxFuel:F1}";

        if (_txtLoaderSpeed != null)
            _txtLoaderSpeed.text = $"적재 속도  {stats.LoaderSpeed:F2}";

        if (_txtDockingSpeed != null)
            _txtDockingSpeed.text = $"도킹 속도  {stats.DockingSpeedThreshold:F1}";
    }
}