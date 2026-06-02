using GameData;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (Instance != null) 
        { 
            Destroy(gameObject); 
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<int>(GameEventType.FoodDelivered, HandleFoodDelivered);
        GameEventBus.Subscribe<int>(GameEventType.IngotRefined, HandleIngotRefined);
        GameEventBus.Subscribe<string>(GameEventType.StageClear, HandleStageClear);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<int>(GameEventType.FoodDelivered, HandleFoodDelivered);
        GameEventBus.Unsubscribe<int>(GameEventType.IngotRefined, HandleIngotRefined);
        GameEventBus.Unsubscribe<string>(GameEventType.StageClear, HandleStageClear);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleFoodDelivered(int amount)
    {
        CheckStackedAchievements(TriggerType.FoodDelivered, amount);
    }

    private void HandleIngotRefined(int amount)
    {
        CheckStackedAchievements(TriggerType.IngotRefined, amount);
    }

    private void HandleStageClear(string stageId)
    {
        CheckStageClearAchievements(stageId);
    }

    // =========================================================================
    // 누적형 업적 처리 (FoodDelivered / IngotRefined)
    // =========================================================================
    private void CheckStackedAchievements(TriggerType triggerType, int amount)
    {
        GameContext context = GameManager.Instance.Context;

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (data.TriggerType != triggerType) continue;
            if (data.AchievementType != AchievementType.Stacked) continue;
            if (context.IsAchievementCompleted(data.Id)) continue;

            // 앞 단계가 완료되지 않은 경우 진행도를 받지 않음
            if (!IsActiveChainStep(data.Id)) continue;

            bool justCompleted = UpdateAchievementProgress(data.Id, amount);
            if (justCompleted)
                NotifyAchievementCompleted(data.Id);
        }
    }

    // =========================================================================
    // 스테이지 클리어 업적 처리
    // =========================================================================
    private void CheckStageClearAchievements(string stageId)
    {
        GameContext context = GameManager.Instance.Context;

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (data.TriggerType != TriggerType.StageClear) continue;
            if (context.IsAchievementCompleted(data.Id)) continue;

            bool stageMatches = string.IsNullOrEmpty(data.StageId) || data.StageId == stageId;
            if (!stageMatches) continue;

            if (data.AchievementType == AchievementType.Stacked)
            {
                bool justCompleted = UpdateAchievementProgress(data.Id, 1);
                if (justCompleted)
                    NotifyAchievementCompleted(data.Id);
            }
            else if (data.AchievementType == AchievementType.Accomplished)
            {
                bool justCompleted = CompleteAchievement(data.Id);
                if (justCompleted)
                    NotifyAchievementCompleted(data.Id);
            }
        }
    }

    // =========================================================================
    // 공통 처리
    // =========================================================================
    private void NotifyAchievementCompleted(string achievementId)
    {
        GameEventBus.Publish(GameEventType.AchievementCompleted, achievementId);
        Debug.Log($"[AchievementManager] 업적 달성: {achievementId}");
    }

    // =========================================================================
    // 업적 처리 메서드
    // =========================================================================
    private bool UpdateAchievementProgress(string achievementId, int addValue)
    {
        GameContext context = GameManager.Instance.Context;

        if (context.IsAchievementCompleted(achievementId)) return false;

        AchievementData data = GameDataManager.Instance.Get<AchievementData>(achievementId);
        if (data == null) return false;

        if (data.AchievementType == AchievementType.Stacked && (data.TargetValue == null || data.TargetValue <= 0))
            return false;

        if (!context.AchievementProgress.ContainsKey(achievementId))
            context.AchievementProgress[achievementId] = 0;

        context.AchievementProgress[achievementId] += addValue;

        if (context.AchievementProgress[achievementId] >= (data.TargetValue ?? 0))
        {
            context.AchievementStatus[achievementId] = true;
            context.AchievementScore += data.Score;
            return true;
        }

        return false;
    }

    private bool CompleteAchievement(string achievementId)
    {
        GameContext context = GameManager.Instance.Context;

        if (context.IsAchievementCompleted(achievementId)) return false;

        context.AchievementStatus[achievementId] = true;
        AchievementData data = GameDataManager.Instance.Get<AchievementData>(achievementId);
        if (data != null)
            context.AchievementScore += data.Score;

        return true;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceCompleteAchievement(string achievementId)
    {
        bool completed = CompleteAchievement(achievementId);
        if (completed)
        {
            NotifyAchievementCompleted(achievementId);
            Debug.Log($"[AchievementManager] 강제 달성: {achievementId}");
        }
    }

    private bool IsActiveChainStep(string achievementId)
    {
        GameContext context = GameManager.Instance.Context;

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (data.NextId != achievementId) continue;

            // 이 업적을 NextId로 가리키는 이전 단계가 완료되지 않았으면 비활성
            if (!context.IsAchievementCompleted(data.Id))
                return false;
        }

        return true;
    }
}