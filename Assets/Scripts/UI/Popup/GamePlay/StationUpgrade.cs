using System.Collections.Generic;
using UnityEngine;
using GameData;

public readonly struct StationUpgradeData
{
    public readonly StationZoneType ZoneType;
    public readonly StationController Station;

    public StationUpgradeData(StationZoneType zoneType, StationController station)
    {
        ZoneType = zoneType;
        Station = station;
    }
}

// 정거장 업그레이드 팝업
public class StationUpgrade : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("업그레이드 아이템")]
    [SerializeField] private GameObject _upgradeItemPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private UpgradeInfo _upgradeInfo;

    // =========================================================================
    // 업그레이드 ID 목록
    // =========================================================================
    private static readonly string[] FarmUpgradeIds = { Upgrade.StatFarm };
    private static readonly string[] RefineUpgradeIds = { Upgrade.StatRefine };

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly List<UpgradeItem> _spawnedItems = new List<UpgradeItem>();

    // =========================================================================
    // 데이터 주입
    // =========================================================================
    public override void Setup(object data = null)
    {
        if (data is not StationUpgradeData upgradeData) return;

        string[] upgradeIds = upgradeData.ZoneType == StationZoneType.Right
            ? FarmUpgradeIds
            : RefineUpgradeIds;

        SpawnItems(upgradeIds, upgradeData.Station);
    }

    protected override void OnBeforeClose()
    {
        ClearItems();
    }

    // =========================================================================
    // 아이템 스폰
    // =========================================================================
    private void SpawnItems(string[] upgradeIds, StationController station)
    {
        ClearItems();

        if (_upgradeItemPrefab == null || _content == null) return;

        foreach (string upgradeId in upgradeIds)
        {
            UpgradeData data = GameDataManager.Instance.Get<UpgradeData>(upgradeId);
            if (data == null) continue;

            for (int slotLevel = 1; slotLevel <= data.MaxLevel; slotLevel++)
            {
                GameObject instance = Instantiate(_upgradeItemPrefab, _content);
                UpgradeItem item = instance.GetComponent<UpgradeItem>();
                if (item == null) continue;

                item.SetInfo(_upgradeInfo);
                item.Setup(upgradeId, slotLevel, station);
                _spawnedItems.Add(item);
            }
        }
    }

    private void ClearItems()
    {
        foreach (UpgradeItem item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }
}