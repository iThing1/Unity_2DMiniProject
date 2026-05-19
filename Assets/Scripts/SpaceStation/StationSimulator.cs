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

    // =========================================================================
    // 내부 스탯
    // =========================================================================
    private float _farmRate;
    private float _refineRate;
    private float _oreToIngotRatio;

    // =========================================================================
    // 업그레이드 / 상수 ID
    // =========================================================================
    private const string UPGRADE_FARM = "UP_Stat_Farm";
    private const string UPGRADE_REFINE = "UP_Stat_Refine";

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Coroutine _farmCoroutine;
    private Coroutine _refineCoroutine;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnUpgradeCompleted += HandleUpgradeCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnUpgradeCompleted -= HandleUpgradeCompleted;
    }

    // =========================================================================
    // 외부 API: 초기화
    // =========================================================================
    public void Initialize()
    {
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

    public void RefundFood(int amount)
    {
        StoredFood += amount;
    }

    public void UnloadOre(float amount)
    {
        StoredOre += amount;
    }

    public void SellIngot()
    {
        if (StoredIngot < 1f)
        {
            Debug.Log("[StationSimulator] 판매할 주괴가 없습니다.");
            return;
        }

        float ingotToSell = Mathf.Floor(StoredIngot);
        float goldEarned = ingotToSell * 10f;   // TODO: 1 주괴당 10 골드로 고정. 밸런스 확인 후 데이터로 이동

        StoredIngot -= ingotToSell;
        GameManager.Instance.TrySpendIngot(ingotToSell);
        GameManager.Instance.AddGold(goldEarned);

        Debug.Log($"[StationSimulator] 주괴 {ingotToSell:F0}개 판매 → 골드 +{goldEarned:F0}");
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
            if (StoredOre >= 10f)
            {
                float refineAmount = Mathf.Min(_refineRate * Time.deltaTime, StoredOre);
                StoredOre -= refineAmount;

                float ingotGain = refineAmount / _oreToIngotRatio;
                StoredIngot += ingotGain;
                GameManager.Instance.AddIngot(ingotGain);
            }

            yield return null;
        }
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleUpgradeCompleted(string upgradeId, int newLevel)
    {
        if (upgradeId == UPGRADE_FARM || upgradeId == UPGRADE_REFINE)
            LoadStats();
    }

    // =========================================================================
    // 스탯 로드
    // =========================================================================
    private void LoadStats()
    {
        _oreToIngotRatio = GameDataManager.Instance.Constants.OreToIngotRatio;
        _farmRate = GameManager.Instance.GetUpgradeStat(UPGRADE_FARM);
        _refineRate = GameManager.Instance.GetUpgradeStat(UPGRADE_REFINE);

        Debug.Log($"[StationSimulator] 스탯 로드 - 농장:{_farmRate:F2}/s, 제련:{_refineRate:F2}/s, 비율:1:{_oreToIngotRatio}");
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