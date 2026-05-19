using GameData;
using System;
using UnityEngine;

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
    public static event Action<int, int, int, int> OnCargoDetailChanged;

    // 행성
    public static event Action<string, PlanetState> OnPlanetStateChanged;
    public static event Action<string, bool> OnPlanetGameOverWarning;
    public static event Action<string> OnPlanetDestroyed;
    public static event Action<string, int> OnFoodDelivered;
    public static event Action<string, int> OnOreCollected;
    public static event Action<string, float> OnPlanetCycleProgress;

    // 우주 정거장
    public static event Action<StationZoneType, bool> OnStationInteractionChanged;
    public static event Action<float, float, float> OnStationStorageChanged;
    public static event Action<float> OnOreUnload;
    public static event Action OnIngotSellRequested;

    // 스폰 관련
    public static event Action<Transform> OnStationSpawned;
    public static event Action<Transform> OnShipSpawned;
    public static event Action<Transform> OnPlanetSpawned;

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
    public static void RaiseCargoDetailChanged(int food, int ore, int total, int capacity) => OnCargoDetailChanged?.Invoke(food, ore, total, capacity);

    public static void RaisePlanetStateChanged(string instanceId, PlanetState state) => OnPlanetStateChanged?.Invoke(instanceId, state);
    public static void RaisePlanetGameOverWarning(string instanceId, bool isWarning) => OnPlanetGameOverWarning?.Invoke(instanceId, isWarning);
    public static void RaisePlanetDestroyed(string instanceId) => OnPlanetDestroyed?.Invoke(instanceId);
    public static void RaiseFoodDelivered(string instanceId, int amount) => OnFoodDelivered?.Invoke(instanceId, amount);
    public static void RaiseOreCollected(string instanceId, int amount) => OnOreCollected?.Invoke(instanceId, amount);
    public static void RaisePlanetCycleProgress(string instanceId, float progress) => OnPlanetCycleProgress?.Invoke(instanceId, progress);

    public static void RaiseStationInteractionChanged(StationZoneType zoneType, bool isActive) => OnStationInteractionChanged?.Invoke(zoneType, isActive);
    public static void RaiseStationStorageChanged(float food, float ore, float ingot) => OnStationStorageChanged?.Invoke(food, ore, ingot);
    public static void RaiseOreUnload(float amount) => OnOreUnload?.Invoke(amount);
    public static void RaiseIngotSellRequested() => OnIngotSellRequested?.Invoke();

    public static void RaiseStationSpawned(Transform stationTransform) => OnStationSpawned?.Invoke(stationTransform);
    public static void RaiseShipSpawned(Transform shipTransform) => OnShipSpawned?.Invoke(shipTransform);
    public static void RaisePlanetSpawned(Transform planetTransform) => OnPlanetSpawned?.Invoke(planetTransform);

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
        OnCargoDetailChanged = null;

        OnPlanetStateChanged = null;
        OnPlanetGameOverWarning = null;
        OnPlanetDestroyed = null;
        OnFoodDelivered = null;
        OnOreCollected = null;
        OnPlanetCycleProgress = null;

        OnStationInteractionChanged = null;
        OnStationStorageChanged = null;
        OnOreUnload = null;
        OnIngotSellRequested = null;

        OnStationSpawned = null;
        OnShipSpawned = null;
        OnPlanetSpawned = null;
    }
}