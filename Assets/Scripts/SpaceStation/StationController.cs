using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameData;

// 우주정거장 오브젝트의 진입점
[RequireComponent(typeof(StationSimulator))]
public class StationController : InteractableBase
{
    [Header("스폰 포인트")]
    [SerializeField] private Transform _spawnPoint;

    public float StoredFood => _simulator.StoredFood;
    public float StoredOre => _simulator.StoredOre;
    public float StoredIngot => _simulator.StoredIngot;

    private StationSimulator _simulator;

    private readonly Dictionary<StationZoneType, StationZone> _zones = new Dictionary<StationZoneType, StationZone>();

    private StationZoneType? _activeZone = null;
    private bool _isGamePlay = false;
    private Coroutine _interactCoroutine;
    private int _pendingFoodLoad;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _simulator = GetComponent<StationSimulator>();

        _zones.Clear();
        foreach (var zone in GetComponentsInChildren<StationZone>())
        {
            if (_zones.ContainsKey(zone.ZoneType))
            {
                Debug.LogWarning($"[StationController] 중복 ZoneType 무시: {zone.ZoneType} ({zone.gameObject.name})");
                continue;
            }
            _zones[zone.ZoneType] = zone;
        }

        if (_zones.Count == 0)
            Debug.LogError("[StationController] 자식에서 StationZone을 찾지 못했습니다.");
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.OnGameStateChanged += HandleGameStateChanged;

        if (GameManager.Instance != null)
            _isGamePlay = GameManager.Instance.CurrentState == GameState.GamePlay;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    public void Initialize()
    {
        _isGamePlay = true;
        _simulator.Initialize();
    }

    public void StopSimulation()
    {
        _simulator.StopProduction();
    }

    // =========================================================================
    // 스폰 포인트
    // =========================================================================
    public void PlacePlayerAtSpawn(Transform playerTransform)
    {
        if (_spawnPoint == null) return;
        playerTransform.position = _spawnPoint.position;
        playerTransform.rotation = _spawnPoint.rotation;
    }

    // =========================================================================
    // 외부 API: 주괴 판매
    // =========================================================================
    public void SellIngot()
    {
        _simulator.SellIngot();
    }

    // =========================================================================
    // 구역 진입 / 이탈
    // =========================================================================
    public void OnPlayerEnterZone(StationZoneType zoneType)
    {
        _activeZone = zoneType;
        _isPlayerInside = true;
    }

    public void OnPlayerExitZone(StationZoneType zoneType)
    {
        if (_activeZone != zoneType) return;
        _activeZone = null;
        _isPlayerInside = false;
        OnDeactivate();
    }

    // =========================================================================
    // InteractableBase 구현
    // =========================================================================
    protected override void OnTriggerEnter2D(Collider2D other) { }
    protected override void OnTriggerExit2D(Collider2D other) { }

    protected override bool CanInteract() => _isGamePlay && _activeZone != null;

    protected override void OnActivate()
    {
        if (_activeZone == null) return;

        GameEvents.RaiseStationInteractionChanged(_activeZone.Value, true);
        StopInteractCoroutine();

        IEnumerator routine = null;

        switch (_activeZone.Value)
        {
            case StationZoneType.Left:
                routine = UnloadOreRoutine();
                break;
            case StationZoneType.Right:
                routine = LoadFoodRoutine();
                break;
            default:
                routine = null;
                break;
        }

        if (routine == null)
        {
            CompleteInteraction();
            return;
        }

        _interactCoroutine = StartCoroutine(routine);
    }

    protected override void OnDeactivate()
    {
        StopInteractCoroutine();

        foreach (StationZoneType z in Enum.GetValues(typeof(StationZoneType)))
            GameEvents.RaiseStationInteractionChanged(z, false);

        _shipInventory?.StopTransfer();
    }

    // =========================================================================
    // 적재 코루틴: 식량 → 우주선
    // =========================================================================
    private IEnumerator LoadFoodRoutine()
    {
        if (_shipInventory == null) { CompleteInteraction(); yield break; }

        int available = Mathf.FloorToInt(_simulator.StoredFood);
        if (available <= 0)
        {
            Debug.Log("[StationController] 적재할 식량이 없습니다.");
            CompleteInteraction();
            yield break;
        }

        int toLoad = Mathf.Min(available, _shipInventory.Capacity - _shipInventory.Count);
        if (toLoad <= 0)
        {
            Debug.Log("[StationController] 우주선 인벤토리가 가득 찼습니다.");
            CompleteInteraction();
            yield break;
        }

        _pendingFoodLoad = toLoad;
        _simulator.TryConsumeFood(toLoad);

        _shipInventory.StartLoading(ShipInventory.CargoType.Food, toLoad, OnFoodLoadComplete);

        yield return new WaitUntil(IsLoadingDone);
        _interactCoroutine = null;
        CompleteInteraction();
    }

    // =========================================================================
    // 하역 코루틴: 우주선 광석 → 정거장 저장소
    // =========================================================================
    private IEnumerator UnloadOreRoutine()
    {
        if (_shipInventory == null) { CompleteInteraction(); yield break; }

        if (_shipInventory.CountOf(ShipInventory.CargoType.Ore) <= 0)
        {
            Debug.Log("[StationController] 하역할 광석이 없습니다.");
            CompleteInteraction();
            yield break;
        }

        _shipInventory.StartUnloading(ShipInventory.CargoType.Ore, OnOreUnloadEach, OnOreUnloadComplete);

        yield return new WaitUntil(IsLoadingDone);
        _interactCoroutine = null;
        CompleteInteraction();
    }

    // =========================================================================
    // 적재/하역 콜백 메서드
    // =========================================================================
    private void OnFoodLoadComplete(int loaded)
    {
        int refund = _pendingFoodLoad - loaded;
        if (refund > 0)
            _simulator.RefundFood(refund);
    }

    private void OnOreUnloadEach()
    {
        _simulator.UnloadOre(1f);
    }

    private void OnOreUnloadComplete(int unloaded) { }

    private bool IsLoadingDone()
    {
        return !_shipInventory.IsLoading;
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        _isGamePlay = next == GameState.GamePlay;

        if (!_isGamePlay)
            OnDeactivate();
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private void StopInteractCoroutine()
    {
        if (_interactCoroutine != null)
        {
            StopCoroutine(_interactCoroutine);
            _interactCoroutine = null;
        }
    }
}