using System;
using GameData;

public static class GameEvents
{
    // 데이터
    public static event Action OnDataInitialized;

    // 게임 상태
    public static event Action<GameState, GameState> OnGameStateChanged;

    // 사운드
    public static event Action<string> OnSFXPlayRequested;

    // 전역 자원
    public static event Action<float> OnGoldChanged;
    public static event Action<float> OnIngotChanged;

    // 업그레이드
    public static event Action<string, int> OnUpgradeCompleted;

    // 스테이지
    public static event Action<string> OnStageSelected;
    public static event Action<string> OnStageClear;
    public static event Action<string> OnStageFailed;

    // =========================================================================
    // null 체크를 한 곳에서 처리
    // =========================================================================
    public static void RaiseDataInitialized() => OnDataInitialized?.Invoke();
    public static void RaiseGameStateChanged(GameState prev, GameState next) => OnGameStateChanged?.Invoke(prev, next);
    public static void RaiseSFXPlayRequested(string address) => OnSFXPlayRequested?.Invoke(address);
    public static void RaiseGoldChanged(float current) => OnGoldChanged?.Invoke(current);
    public static void RaiseIngotChanged(float current) => OnIngotChanged?.Invoke(current);
    public static void RaiseUpgradeCompleted(string upgradeId, int lv) => OnUpgradeCompleted?.Invoke(upgradeId, lv);
    public static void RaiseStageSelected(string stageId) => OnStageSelected?.Invoke(stageId);
    public static void RaiseStageClear(string stageId) => OnStageClear?.Invoke(stageId);
    public static void RaiseStageFailed(string stageId) => OnStageFailed?.Invoke(stageId);

    public static void ClearAllListeners()
    {
        OnDataInitialized = null;
        OnGameStateChanged = null;
        OnSFXPlayRequested = null;
        OnGoldChanged = null;
        OnIngotChanged = null;
        OnUpgradeCompleted = null;
        OnStageSelected = null;
        OnStageClear = null;
        OnStageFailed = null;
    }
}