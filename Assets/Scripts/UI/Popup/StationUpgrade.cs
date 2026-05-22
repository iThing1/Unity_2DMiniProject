using System.Collections.Generic;
using UnityEngine;
using GameData;

// 정거장 업그레이드 팝업
// Left Zone: 광석(제련) 관련 업그레이드
// Right Zone: 식량(농장) 관련 업그레이드
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

    private static readonly string[] FarmUpgradeIds = { "UP_Stat_Farm" };
    private static readonly string[] RefineUpgradeIds = { "UP_Stat_Refine" };

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly List<UpgradeItem> _spawnedItems = new List<UpgradeItem>();
    private StationController _station;

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Open(StationZoneType zoneType, StationController station)
    {
        _station = station;

        string[] upgradeIds = zoneType == StationZoneType.Right
            ? FarmUpgradeIds
            : RefineUpgradeIds;

        SpawnItems(upgradeIds);
        gameObject.SetActive(true);
    }

    protected override void OnBeforeClose()
    {
        ClearItems();
    }

    // =========================================================================
    // 아이템 스폰
    // =========================================================================
    private void SpawnItems(string[] upgradeIds)
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
                item.Setup(upgradeId, slotLevel, _station);
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