using GameData;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // =========================================================================
    // 팝업 큐
    // =========================================================================
    private readonly Queue<AchievementData> _popupQueue = new Queue<AchievementData>();
    private bool _isShowingPopup = false;

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
        GameEventBus.Subscribe<int>(GameEventType.SpeedReached, HandleSpeedReached);
        GameEventBus.Subscribe<string>(GameEventType.UpgradeCompleted, HandleUpgradeCompleted);
        GameEventBus.Subscribe(GameEventType.HiddenCodeFound, HandleHiddenCodeFound);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<int>(GameEventType.FoodDelivered, HandleFoodDelivered);
        GameEventBus.Unsubscribe<int>(GameEventType.IngotRefined, HandleIngotRefined);
        GameEventBus.Unsubscribe<string>(GameEventType.StageClear, HandleStageClear);
        GameEventBus.Unsubscribe<int>(GameEventType.SpeedReached, HandleSpeedReached);
        GameEventBus.Unsubscribe<string>(GameEventType.UpgradeCompleted, HandleUpgradeCompleted);
        GameEventBus.Unsubscribe(GameEventType.HiddenCodeFound, HandleHiddenCodeFound);
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

    private void HandleSpeedReached(int speed)
    {
        CheckThresholdAchievements(TriggerType.SpeedReached, speed);
    }

    private void HandleUpgradeCompleted(string upgradeId)
    {
        CheckStackedAchievements(TriggerType.UpgradeCompleted, 1);
    }

    private void HandleHiddenCodeFound()
    {
        CheckAccomplishedAchievements(TriggerType.HiddenCodeFound);
    }

    // =========================================================================
    // 누적형 업적 처리 (FoodDelivered / IngotRefined / UpgradeCompleted)
    // =========================================================================
    private void CheckStackedAchievements(TriggerType triggerType, int amount)
    {
        GameContext context = GameManager.Instance.Context;

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (data.TriggerType != triggerType) continue;
            if (data.AchievementType != AchievementType.Stacked) continue;
            if (context.IsAchievementCompleted(data.Id)) continue;

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
    // 도달형 업적 처리 (SpeedReached)
    // =========================================================================
    private void CheckThresholdAchievements(TriggerType triggerType, int currentValue)
    {
        GameContext context = GameManager.Instance.Context;

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (data.TriggerType != triggerType) continue;
            if (data.AchievementType != AchievementType.Stacked) continue;
            if (context.IsAchievementCompleted(data.Id)) continue;
            if (!IsActiveChainStep(data.Id)) continue;

            if (currentValue >= (data.TargetValue ?? 0))
            {
                context.AchievementProgress[data.Id] = currentValue;
                bool justCompleted = CompleteAchievement(data.Id);
                if (justCompleted)
                    NotifyAchievementCompleted(data.Id);
            }
        }
    }

    // =========================================================================
    // 달성형 업적 처리 (HiddenCodeFound)
    // =========================================================================
    private void CheckAccomplishedAchievements(TriggerType triggerType)
    {
        GameContext context = GameManager.Instance.Context;

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (data.TriggerType != triggerType) continue;
            if (data.AchievementType != AchievementType.Accomplished) continue;
            if (context.IsAchievementCompleted(data.Id)) continue;

            bool justCompleted = CompleteAchievement(data.Id);
            if (justCompleted)
                NotifyAchievementCompleted(data.Id);
        }
    }
    // =========================================================================
    // 공통 처리
    // =========================================================================
    private void NotifyAchievementCompleted(string achievementId)
    {
        GameEventBus.Publish(GameEventType.AchievementCompleted, achievementId);

        AchievementData data = GameDataManager.Instance.Get<AchievementData>(achievementId);
        if (data != null)
        {
            _popupQueue.Enqueue(data);
            TryShowNextPopup();
        }
    }

    // =========================================================================
    // 팝업 큐 처리
    // =========================================================================
    private void TryShowNextPopup()
    {
        if (_isShowingPopup) return;
        if (_popupQueue.Count == 0) return;

        AchievementData data = _popupQueue.Dequeue();
        _isShowingPopup = true;

        AchieveAccomplished popup = UIManager.Instance.PrepareUI<AchieveAccomplished>(UIId.Popup.Accomplished);
        if (popup == null)
        {
            _isShowingPopup = false;
            TryShowNextPopup();
            return;
        }

        popup.Show(data);
    }

    public void OnPopupClosed()
    {
        _isShowingPopup = false;
        TryShowNextPopup();
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

    public void ForceCompleteAchievement(string achievementId)
    {
        bool completed = CompleteAchievement(achievementId);
        if (completed)
        {
            NotifyAchievementCompleted(achievementId);
        }
    }

    private bool IsActiveChainStep(string achievementId)
    {
        GameContext context = GameManager.Instance.Context;

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (data.NextId != achievementId) continue;

            if (!context.IsAchievementCompleted(data.Id))
                return false;
        }

        return true;
    }
}