using UnityEngine;

// 우주선 업그레이드 패널
// Lobby에서 버튼 클릭 시 오픈
public class UpgradeUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private ShipUpgrade _shipUpgrade;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        _shipUpgrade?.Open();
    }

    protected override void OnBeforeClose()
    {
        _shipUpgrade?.Close();
    }
}