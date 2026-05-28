using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameData;

// 정거장 내부 시뮬레이션을 담당
public class StationSimulator : MonoBehaviour
{
    // =========================================================================
    // 런타임 저장소 (외부 읽기용)
    // =========================================================================
    public float StoredFood { get; private set; }
    public float StoredOre { get; private set; }
    public float StoredIngot { get; private set; }

    public float FarmProgress => _farmRate > 0f ? Mathf.Repeat(StoredFood, 1f) : 0f;
    public float RefineProgress => _oreToIngotRatio > 0f ? Mathf.Clamp01(_refineAccumulator / _oreToIngotRatio) : 0f;
    public bool IsRefining => StoredOre >= _oreToIngotRatio;

    // =========================================================================
    // 내부 스탯
    // =========================================================================
    private float _farmRate;
    private float _refineRate;
    private float _oreToIngotRatio;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Coroutine _farmCoroutine;
    private Coroutine _refineCoroutine;
    private float _refineAccumulator;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEventBus.Subscribe<StationStats>(GameEventType.StationStatsChanged, HandleStationStatsChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<StationStats>(GameEventType.StationStatsChanged, HandleStationStatsChanged);
    }

    // =========================================================================
    // 외부 API: 초기화
    // =========================================================================
    public void Initialize()
    {
        _refineAccumulator = 0f;
        LoadStats();
        ResetTempUpgrades();
        StartProduction();
    }

    public void StopProduction()
    {
        StopFarm();
        StopRefine();
    }

    // =========================================================================
    // 외부 API: 저장소 접근
    // =========================================================================
    public void AddOre(float amount)
    {
        StoredOre += amount;
    }

    public bool TryConsumeFood(int amount)
    {
        if (StoredFood < amount) return false;
        StoredFood -= amount;
        StoredFood = Mathf.Max(0f, StoredFood);
        return true;
    }

    public bool TryConsumeOre(float amount)
    {
        if (StoredOre < amount) return false;
        StoredOre -= amount;
        StoredOre = Mathf.Max(0f, StoredOre);
        return true;
    }

    public void RefundFood(int amount)
    {
        StoredFood += amount;
    }

    public void UnloadOre(float amount)
    {
        StoredOre += amount;
    }

    // =========================================================================
    // 생산 루프: 농장
    // =========================================================================
    private IEnumerator FarmRoutine()
    {
        while (true)
        {
            StoredFood += _farmRate * Time.deltaTime;
            yield return null;
        }
    }

    // =========================================================================
    // 생산 루프: 제련
    // =========================================================================
    private IEnumerator RefineRoutine()
    {
        while (true)
        {
            if (StoredOre >= _oreToIngotRatio)
            {
                float refineAmount = Mathf.Min(_refineRate * Time.deltaTime, StoredOre);
                StoredOre -= refineAmount;

                _refineAccumulator += refineAmount;
                int ingotGain = Mathf.FloorToInt(_refineAccumulator / _oreToIngotRatio);

                if (ingotGain > 0)
                {
                    _refineAccumulator -= ingotGain * _oreToIngotRatio;
                    StoredIngot += ingotGain;
                    GameManager.Instance.AddIngot(ingotGain);
                    GameEventBus.Publish(GameEventType.IngotRefined, ingotGain);
                }
            }

            yield return null;
        }
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStationStatsChanged(StationStats stats)
    {
        ApplyStats(stats);
    }

    // =========================================================================
    // 스탯 로드
    // =========================================================================
    private void LoadStats()
    {
        _oreToIngotRatio = GameDataManager.Instance.Constants.OreToIngotRatio;
        ApplyStats(GameManager.Instance.SetStationStats());
    }

    // 스탯 적용
    private void ApplyStats(StationStats stats)
    {
        _farmRate = stats.FarmRate;
        _refineRate = stats.RefineRate;
    }

    // =========================================================================
    // TEMP 업그레이드 초기화
    // =========================================================================
    public void ResetTempUpgrades()
    {
        var context = GameManager.Instance.Context;
        var dm = GameDataManager.Instance;
        var keysToReset = new List<string>();

        foreach (var kvp in context.UpgradeLevels)
        {
            var data = dm.Get<UpgradeData>(kvp.Key);
            if (data != null && data.UpgradeType == "TEMP")
                keysToReset.Add(kvp.Key);
        }

        foreach (var key in keysToReset)
            context.UpgradeLevels[key] = 0;

        if (keysToReset.Count > 0)
        {
            LoadStats();
            Debug.Log($"[StationSimulator] 임시 업그레이드 초기화");
        }
    }

    // =========================================================================
    // 루프 제어
    // =========================================================================
    private void StartProduction()
    {
        StopFarm();
        StopRefine();
        _farmCoroutine = StartCoroutine(FarmRoutine());
        _refineCoroutine = StartCoroutine(RefineRoutine());
    }

    private void StopFarm()
    {
        if (_farmCoroutine != null)
        {
            StopCoroutine(_farmCoroutine);
            _farmCoroutine = null;
        }
    }

    private void StopRefine()
    {
        if (_refineCoroutine != null)
        {
            StopCoroutine(_refineCoroutine);
            _refineCoroutine = null;
        }
    }
}