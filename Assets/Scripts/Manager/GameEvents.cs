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
    public static event Action OnStageStartRequested;
    public static event Action<string> OnStageClear;
    public static event Action<string> OnStageFailed;

    // 행성
    public static event Action<string, bool> OnPlanetGameOverWarning;
    public static event Action<string> OnPlanetDestroyed;

    // 우주 정거장
    public static event Action<StationZoneType, bool> OnStationInteractionChanged;

    // 스폰 관련
    public static event Action<Transform> OnStationSpawned;
    public static event Action<Transform> OnShipSpawned;
    public static event Action<Transform> OnPlanetSpawned;

    // =========================================================================
    // 이벤트 처리
    // =========================================================================
    public static void RaiseDataInitialized() => OnDataInitialized?.Invoke();
    public static void RaiseGameStateChanged(GameState prev, GameState next) => OnGameStateChanged?.Invoke(prev, next);

    public static void RaiseSFXPlayRequested(string address) => OnSFXPlayRequested?.Invoke(address);

    public static void RaiseGoldChanged(float current) => OnGoldChanged?.Invoke(current);
    public static void RaiseIngotChanged(float current) => OnIngotChanged?.Invoke(current);

    public static void RaiseUpgradeCompleted(string upgradeId, int lv) => OnUpgradeCompleted?.Invoke(upgradeId, lv);

    public static void RaiseStageSelected(string stageId) => OnStageSelected?.Invoke(stageId);
    public static void RaiseStageStartRequested() => OnStageStartRequested?.Invoke();
    public static void RaiseStageClear(string stageId) => OnStageClear?.Invoke(stageId);
    public static void RaiseStageFailed(string stageId) => OnStageFailed?.Invoke(stageId);

    public static void RaisePlanetGameOverWarning(string instanceId, bool isWarning) => OnPlanetGameOverWarning?.Invoke(instanceId, isWarning);
    public static void RaisePlanetDestroyed(string instanceId) => OnPlanetDestroyed?.Invoke(instanceId);

    public static void RaiseStationInteractionChanged(StationZoneType zoneType, bool isActive) => OnStationInteractionChanged?.Invoke(zoneType, isActive);

    public static void RaiseStationSpawned(Transform stationTransform) => OnStationSpawned?.Invoke(stationTransform);
    public static void RaiseShipSpawned(Transform shipTransform) => OnShipSpawned?.Invoke(shipTransform);
    public static void RaisePlanetSpawned(Transform planetTransform) => OnPlanetSpawned?.Invoke(planetTransform);

    // null 초기화 및 모든 리스너 제거
    public static void ClearAllListeners()
    {
        OnDataInitialized = null;
        OnGameStateChanged = null;

        OnSFXPlayRequested = null;

        OnGoldChanged = null;
        OnIngotChanged = null;

        OnUpgradeCompleted = null;

        OnStageSelected = null;
        OnStageStartRequested = null;
        OnStageClear = null;
        OnStageFailed = null;

        OnPlanetGameOverWarning = null;
        OnPlanetDestroyed = null;

        OnStationInteractionChanged = null;

        OnStationSpawned = null;
        OnShipSpawned = null;
        OnPlanetSpawned = null;
    }
}