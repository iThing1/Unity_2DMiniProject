using GameData;
using UnityEngine;

// 재화 전담 클래스
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

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

    // =========================================================================
    // 재화 접근
    // =========================================================================
    public void AddGold(int amount)
    {
        GameContext context = GameManager.Instance.Context;
        context.CurrentGold += amount;
        GameEventBus.Publish(GameEventType.GoldChanged, context.CurrentGold);

        if (GameManager.Instance.CurrentState == GameState.GamePlay)
        {
            StageManager.Instance.StageEarnedGold += amount;
            CheckStageClear();
        }
    }

    public bool TrySpendGold(int amount)
    {
        GameContext context = GameManager.Instance.Context;
        if (context.CurrentGold < amount) return false;
        context.CurrentGold -= amount;
        GameEventBus.Publish(GameEventType.GoldChanged, context.CurrentGold);
        return true;
    }

    public void AddIngot(int amount)
    {
        GameContext context = GameManager.Instance.Context;
        context.CurrentIngot += amount;
        GameEventBus.Publish(GameEventType.IngotChanged, context.CurrentIngot);

        if (GameManager.Instance.CurrentState == GameState.GamePlay)
            StageManager.Instance.StageEarnedIngot += amount;
    }

    public bool TrySpendIngot(int amount)
    {
        GameContext context = GameManager.Instance.Context;
        if (context.CurrentIngot < amount) return false;
        context.CurrentIngot -= amount;
        GameEventBus.Publish(GameEventType.IngotChanged, context.CurrentIngot);
        return true;
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private void CheckStageClear()
    {
        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        if (string.IsNullOrEmpty(stageId)) return;

        StageData data = GameDataManager.Instance.Get<StageData>(stageId);
        if (data == null) return;

        if (GameManager.Instance.Context.CurrentGold >= data.ReqGold)
            GameEventBus.Publish(GameEventType.StageClearCondition);
    }
}