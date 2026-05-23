using GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnShipStatsChanged += HandleShipStatsChanged;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnShipSpawned += HandleShipSpawned;

        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInitialized)
        {
            _transferInterval = GameDataManager.Instance.Constants.CargoTransferInterval;
            ApplyCapacity(GameManager.Instance.SetShipStats().Capacity);
        }
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnShipStatsChanged -= HandleShipStatsChanged;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnShipSpawned -= HandleShipSpawned;
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    public void InitializeStats()
    {
        _transferInterval = GameDataManager.Instance.Constants.CargoTransferInterval;
        ApplyCapacity(GameManager.Instance.SetShipStats().Capacity);
    }

    private void ApplyCapacity(int capacity)
    {
        Capacity = capacity;

        if (Capacity <= 0)
            Debug.LogWarning($"[ShipInventory] Capacity가 0 이하입니다. 데이터 확인 필요.");
    }

    // =========================================================================
    // 외부 API: 화물 추가/제거/조회
    // =========================================================================
    public bool TryAdd(CargoType type)
    {
        if (IsFull) return false;

        _cargo.Add(new Cargo(type));
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
    public void StartLoading(CargoType type, int totalAmount, Action onEach, Action<int> onComplete = null)
    {
        StopLoading();
        _loadCoroutine = StartCoroutine(LoadRoutine(type, totalAmount, onEach, onComplete));
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
    private IEnumerator LoadRoutine(CargoType type, int totalAmount, Action onEach, Action<int> onComplete)
    {
        int loaded = 0;
        var wait = new WaitForSeconds(_transferInterval);

        while (loaded < totalAmount && !IsFull)
        {
            if (TryAdd(type))
            {
                loaded++;
                onEach?.Invoke();
            }

            yield return wait;
        }

        _loadCoroutine = null;
        onComplete?.Invoke(loaded);
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
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleDataInitialized()
    {
        InitializeStats();
        Debug.Log($"[CargoInventory] 초기화 완료. 용량: {Capacity}, 인터벌: {_transferInterval}s");
    }

    private void HandleShipStatsChanged(ShipStats stats)
    {
        ApplyCapacity(stats.Capacity);
        Debug.Log($"[ShipInventory] 용량 갱신. {Capacity}");
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (prev == GameState.GamePlay)
        {
            StopTransfer();
            _cargo.Clear();
        }  
    }

    private void HandleShipSpawned(Transform shipTransform)
    {
        InitializeStats();
        Debug.Log($"[ShipInventory] 스폰 후 초기화 완료. 용량: {Capacity}");
    }
}