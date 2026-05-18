using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameData;

public class ShipInventory : MonoBehaviour
{
    public enum CargoType { Food, Ore }

    public struct Cargo
    {
        public CargoType Type;
        public int Amount;

        public Cargo(CargoType type, int amount = 1)
        {
            Type = type;
            Amount = amount;
        }
    }

    // =========================================================================
    // 외부 읽기용
    // =========================================================================
    public int Count => _cargo.Count;
    public int Capacity { get; private set; }

    public bool IsFull => _cargo.Count >= Capacity;
    public bool IsEmpty => _cargo.Count == 0;
    public bool IsLoading => _loadCoroutine != null || _unloadCoroutine != null;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly List<Cargo> _cargo = new List<Cargo>();

    private float _transferInterval;
    private Coroutine _loadCoroutine;
    private Coroutine _unloadCoroutine;

    // 업그레이드 ID 상수
    private const string UPGRADE_CARGO = "UP_Ship_Cargo";

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnUpgradeCompleted += HandleUpgradeCompleted;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnUpgradeCompleted -= HandleUpgradeCompleted;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    private void HandleDataInitialized()
    {
        _transferInterval = GameDataManager.Instance.Constants.CargoTransferInterval;

        RefreshCapacity();
        BroadcastState();
        Debug.Log($"[CargoInventory] 초기화 완료. 용량: {Capacity}, 인터벌: {_transferInterval}s");
    }

    private void RefreshCapacity()
    {
        float stat = GameManager.Instance.GetUpgradeStat(UPGRADE_CARGO);
        Capacity = stat > 0f ? Mathf.RoundToInt(stat) : 10;
    }

    // =========================================================================
    // 외부 API: 화물 추가/제거/조회
    // =========================================================================

    public bool TryAdd(CargoType type)
    {
        if (IsFull) return false;

        _cargo.Add(new Cargo(type));
        BroadcastState();
        return true;
    }

    public bool TryRemove(CargoType type)
    {
        int idx = -1;
        for (int i = _cargo.Count - 1; i >= 0; i--)
        {
            if (_cargo[i].Type == type)
            {
                idx = i;
                break;
            }
        }
        if (idx < 0) return false;

        _cargo.RemoveAt(idx);
        BroadcastState();
        return true;
    }

    public int CountOf(CargoType type)
    {
        int n = 0;
        foreach (var c in _cargo)
            if (c.Type == type) n += c.Amount;
        return n;
    }

    public IReadOnlyList<Cargo> GetAll() => _cargo;

    // =========================================================================
    // 외부 API: 코루틴 적재 / 하역
    // =========================================================================

    public void StartLoading(CargoType type, int totalAmount, Action<int> onComplete = null)
    {
        StopLoading();
        _loadCoroutine = StartCoroutine(LoadRoutine(type, totalAmount, onComplete));
    }

    public void StartUnloading(CargoType type, Action onEach = null, Action<int> onComplete = null)
    {
        StopUnloading();
        _unloadCoroutine = StartCoroutine(UnloadRoutine(type, onEach, onComplete));
    }

    public void StopLoading()
    {
        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
            _loadCoroutine = null;
        }
    }

    public void StopUnloading()
    {
        if (_unloadCoroutine != null)
        {
            StopCoroutine(_unloadCoroutine);
            _unloadCoroutine = null;
        }
    }

    public void StopTransfer()
    {
        StopLoading();
        StopUnloading();
    }

    // =========================================================================
    // 코루틴 구현
    // =========================================================================
    private IEnumerator LoadRoutine(CargoType type, int totalAmount, Action<int> onComplete)
    {
        int loaded = 0;
        var wait = new WaitForSeconds(_transferInterval);

        while (loaded < totalAmount && !IsFull)
        {
            if (TryAdd(type))
                loaded++;

            yield return wait;
        }

        _loadCoroutine = null;
        onComplete?.Invoke(loaded);

        Debug.Log($"[CargoInventory] 적재 완료. {type} x{loaded}");
    }

    private IEnumerator UnloadRoutine(CargoType type, Action onEach, Action<int> onComplete)
    {
        int unloaded = 0;
        var wait = new WaitForSeconds(_transferInterval);

        while (CountOf(type) > 0)
        {
            if (TryRemove(type))
            {
                unloaded++;
                onEach?.Invoke();
            }

            yield return wait;
        }

        _unloadCoroutine = null;
        onComplete?.Invoke(unloaded);

        Debug.Log($"[CargoInventory] 하역 완료. {type} x {unloaded}");
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleUpgradeCompleted(string upgradeId, int newLevel)
    {
        if (upgradeId != UPGRADE_CARGO) return;
        RefreshCapacity();
        BroadcastState();
        Debug.Log($"[CargoInventory] 용량 갱신. {Capacity}");
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (prev == GameState.GamePlay)
            StopTransfer();
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private void BroadcastState()
    {
        GameEvents.RaiseCargoChanged(Count, Capacity);
        GameEvents.RaiseCargoDetailChanged(CountOf(CargoType.Food), CountOf(CargoType.Ore), Count, Capacity);
    }
}