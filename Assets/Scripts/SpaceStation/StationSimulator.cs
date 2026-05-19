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
    private Coroutine _idleCoroutine;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnUpgradeCompleted += HandleUpgradeCompleted;
        GameEvents.OnOreUnload += HandleOreUnload;
        GameEvents.OnIngotSellRequested += HandleIngotSellRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnUpgradeCompleted -= HandleUpgradeCompleted;
        GameEvents.OnOreUnload -= HandleOreUnload;
        GameEvents.OnIngotSellRequested -= HandleIngotSellRequested;
    }

    // =========================================================================
    // 외부 API: 초기화
    // =========================================================================
    public void Initialize()
    {
        LoadStats();
        ResetTempUpgrades();
        StartIdleProduction();
        BroadcastStorage();
    }

    public void StopProduction()
    {
        StopIdleProduction();
    }

    // =========================================================================
    // 외부 API: 저장소 접근
    // =========================================================================
    public void AddOre(float amount)
    {
        StoredOre += amount;
        BroadcastStorage();
    }

    public bool TryConsumeFood(int amount)
    {
        if (StoredFood < amount) return false;
        StoredFood -= amount;
        StoredFood = Mathf.Max(0f, StoredFood);
        BroadcastStorage();
        return true;
    }

    public void RefundFood(int amount)
    {   
        StoredFood += amount;
        BroadcastStorage();
    }

    // =========================================================================
    // Idle 생산 루프
    // =========================================================================
    private IEnumerator IdleProductionRoutine()
    {
        while (true)
        {
            float dt = Time.deltaTime;
            StoredFood += _farmRate * dt;

            if (StoredOre >= 10f)
            {
                float refineAmount = Mathf.Min(_refineRate * dt, StoredOre);
                StoredOre -= refineAmount;
                StoredIngot += refineAmount / _oreToIngotRatio;
                GameManager.Instance.Context.CurrentIngot += StoredIngot;
            }

            BroadcastStorage();
            yield return null;
        }
    }

    // =========================================================================
    // 주괴 판매
    // =========================================================================
    private void HandleIngotSellRequested()
    {
        if (StoredIngot < 1f)
        {
            Debug.Log("[StationSimulator] 판매할 주괴가 없습니다.");
            return;
        }

        float ingotToSell = Mathf.Floor(StoredIngot);
        float goldEarned = ingotToSell * 10f;   // TODO: 1 주괴당 10 골드로 고정. 밸런스 확인 후 데이터로 이동

        StoredIngot -= ingotToSell;
        GameManager.Instance.AddGold(goldEarned);
        BroadcastStorage();

        Debug.Log($"[StationSimulator] 주괴 {ingotToSell:F0}개 판매 → 골드 +{goldEarned:F0}");
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleUpgradeCompleted(string upgradeId, int newLevel)
    {
        if (upgradeId == UPGRADE_FARM || upgradeId == UPGRADE_REFINE)
            LoadStats();
    }

    private void HandleOreUnload(float amount)
    {
        StoredOre += amount;
        BroadcastStorage();
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
    // Idle 루프 제어
    // =========================================================================
    private void StartIdleProduction()
    {
        StopIdleProduction();
        _idleCoroutine = StartCoroutine(IdleProductionRoutine());
    }

    private void StopIdleProduction()
    {
        if (_idleCoroutine != null)
        {
            StopCoroutine(_idleCoroutine);
            _idleCoroutine = null;
        }
    }

    // =========================================================================
    // 브로드캐스트
    // =========================================================================
    public void BroadcastStorage()
        => GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);

    // =========================================================================
    // 디버그
    // =========================================================================
#if UNITY_EDITOR
    [ContextMenu("디버그: 식량 100 추가")]
    private void Debug_AddFood() { StoredFood += 100f; BroadcastStorage(); }

    [ContextMenu("디버그: 광석 50 추가")]
    private void Debug_AddOre() { StoredOre += 50f; BroadcastStorage(); }

    [ContextMenu("디버그: 주괴 10 추가")]
    private void Debug_AddIngot() { StoredIngot += 10f; BroadcastStorage(); }

    [ContextMenu("디버그: 주괴 전량 판매")]
    private void Debug_SellIngot() => HandleIngotSellRequested();

    [ContextMenu("디버그: 현재 상태 출력")]
    private void Debug_PrintState()
    {
        Debug.Log($"[StationSimulator] 식량:{StoredFood:F1} | 광석:{StoredOre:F1} | 주괴:{StoredIngot:F2} | 농장:{_farmRate:F2}/s | 제련:{_refineRate:F2}/s");
    }
#endif
}