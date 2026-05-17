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

    // 우주선
    public static event Action<float, float> OnFuelChanged;
    public static event Action<bool> OnOverheatChanged;
    public static event Action<bool> OnBoosterChanged;

    public static event Action<int, int> OnCargoChanged;

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

    public static void RaiseFuelChanged(float current, float max) => OnFuelChanged?.Invoke(current, max);
    public static void RaiseOverheatChanged(bool isOverheat) => OnOverheatChanged?.Invoke(isOverheat);
    public static void RaiseBoosterChanged(bool isActive) => OnBoosterChanged?.Invoke(isActive);

    public static void RaiseCargoChanged(int count, int capacity) => OnCargoChanged?.Invoke(count, capacity);

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

        OnFuelChanged = null;
        OnOverheatChanged = null;
        OnBoosterChanged = null;

        OnCargoChanged = null;
    }
}