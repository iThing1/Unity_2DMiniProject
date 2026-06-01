using GameData;
using System;
using System.Collections.Generic;
using UnityEngine;

// 스테이지 진행 전담 클래스
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    // =========================================================================
    // 프로퍼티
    // =========================================================================
    public float StageStartTime { get; private set; }
    public int StageEarnedGold { get; set; }
    public int StageEarnedIngot { get; set; }

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<string>(GameEventType.StageClear, HandleStageClear);
        GameEventBus.Subscribe<string>(GameEventType.StageFailed, HandleStageFailed);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.StageClear, HandleStageClear);
        GameEventBus.Unsubscribe<string>(GameEventType.StageFailed, HandleStageFailed);
    }

    // =========================================================================
    // 스테이지 시작 초기화
    // =========================================================================
    public void OnEnterGamePlay()
    {
        StageStartTime = Time.realtimeSinceStartup;
        StageEarnedGold = 0;
        StageEarnedIngot = 0;
    }

    // =========================================================================
    // 스테이지 클리어 요청
    // =========================================================================
    public void TryClearStage(string stageId)
    {
        StageData data = GameDataManager.Instance.Get<StageData>(stageId);
        if (data == null) return;

        if (CurrencyManager.Instance.TrySpendGold(data.ReqGold))
            GameEventBus.Publish(GameEventType.StageClear, stageId);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStageClear(string stageId)
    {
        GameContext context = GameManager.Instance.Context;
        context.StageClearStatus[stageId] = true;

        if (!context.UnlockedStageIds.Contains(stageId))
            context.UnlockedStageIds.Add(stageId);

        int goldEarned = StageEarnedGold;
        int ingotEarned = StageEarnedIngot;

        StageClearRecord record = new StageClearRecord
        {
            ClearTime = Time.realtimeSinceStartup - StageStartTime,
            GoldEarned = goldEarned,
            IngotEarned = ingotEarned,
            Score = goldEarned * GameConfig.GoldScoreMultiplier + ingotEarned * GameConfig.IngotScoreMultiplier,
            ClearedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        if (!context.ClearRecords.ContainsKey(stageId))
            context.ClearRecords[stageId] = new List<StageClearRecord>();

        context.ClearRecords[stageId].Add(record);

        Time.timeScale = 0f;
        StageClear stageClear = UIManager.Instance.PrepareUI<StageClear>(UIId.Popup.StageClear);
        if (stageClear != null)
        {
            stageClear.Setup(record);
            UIManager.Instance.OpenUI(UIId.Popup.StageClear);
        }
    }

    private void HandleStageFailed(string stageId)
    {
        GameManager.Instance.Context.StageClearStatus[stageId] = false;

        Time.timeScale = 0f;
        UIManager.Instance.OpenUI(UIId.Popup.StageFailed);
    }
}