using System.Collections.Generic;
using UnityEngine;
using GameData;

public class Scoreboard : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("아이템")]
    [SerializeField] private GameObject _rankItemPrefab;
    [SerializeField] private Transform _content;

    private const int MaxDisplayCount = 5;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly List<RankItem> _spawnedItems = new List<RankItem>();

    // =========================================================================
    // 데이터 주입
    // =========================================================================
    public override void Setup(object data = null)
    {
        if (data is not string stageId) return;

        ClearItems();

        List<StageClearRecord> records = GetSortedRecords(stageId);

        int displayCount = Mathf.Min(records.Count, MaxDisplayCount);
        for (int i = 0; i < displayCount; i++)
        {
            GameObject instance = Instantiate(_rankItemPrefab, _content);
            RankItem item = instance.GetComponent<RankItem>();
            if (item == null) continue;

            item.Setup(i + 1, records[i]);
            _spawnedItems.Add(item);
        }
    }

    protected override void OnBeforeClose()
    {
        ClearItems();
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private List<StageClearRecord> GetSortedRecords(string stageId)
    {
        var context = GameManager.Instance.Context;

        if (!context.ClearRecords.TryGetValue(stageId, out List<StageClearRecord> records))
            return new List<StageClearRecord>();

        List<StageClearRecord> sorted = new List<StageClearRecord>(records);
        sorted.Sort(CompareByScoreDesc);
        return sorted;
    }

    private int CompareByScoreDesc(StageClearRecord a, StageClearRecord b)
    {
        return b.Score.CompareTo(a.Score);
    }

    private void ClearItems()
    {
        foreach (RankItem item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }
}