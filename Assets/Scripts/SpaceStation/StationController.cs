using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameData;

public class StationController : MonoBehaviour
{
    // =========================================================================
    // 인스펙터 창
    // =========================================================================
    [Header("Player SpawnPoint")]
    [SerializeField] private Transform _spawnPoint;

    private readonly Dictionary<StationZoneType, StationZone> _zones
        = new Dictionary<StationZoneType, StationZone>();

    // =========================================================================
    // 내부 스탯 (업그레이드 반영)
    // =========================================================================
    public float StoredFood { get; private set; }
    public float StoredOre { get; private set; }
    public float StoredIngot { get; private set; }

    private float _farmRate;
    private float _refineRate;
    private float _oreToIngotRatio;

    // =========================================================================
    // 상태
    // =========================================================================
    private ShipController _shipController;
    private ShipInventory _shipInventory;

    private StationZoneType? _activeZone = null;
    private bool _isInteracting = false;

    private bool _isGamePlay = false;

    private Coroutine _idleCoroutine;
    private Coroutine _interactCoroutine;

    // =========================================================================
    // 업그레이드 상수
    // =========================================================================
    private const string UPGRADE_REFINE = "UP_Stat_Refine";
    private const string UPGRADE_FARM = "UP_Stat_Farm";
    private const string CONST_ORE_TO_INGOT = "ORE_TO_INGOT_RATIO";

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _zones.Clear();
        foreach (var zone in GetComponentsInChildren<StationZone>())
        {
            if (_zones.ContainsKey(zone.ZoneType))
            {
                Debug.LogWarning($"[StationController] 중복 ZoneType 무시: {zone.ZoneType} ({zone.gameObject.name})");
                continue;
            }
            _zones[zone.ZoneType] = zone;
            Debug.Log($"[StationController] 구역 등록: {zone.ZoneType} <- {zone.gameObject.name}");
        }

        if (_zones.Count == 0)
            Debug.LogError("[StationController] ZoneType을 설정하세요.");
    }

    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnUpgradeCompleted += HandleUpgradeCompleted;
        GameEvents.OnOreUnload += HandleOreUnload;
        GameEvents.OnIngotSellRequested += HandleIngotSellRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnUpgradeCompleted -= HandleUpgradeCompleted;
        GameEvents.OnOreUnload -= HandleOreUnload;
        GameEvents.OnIngotSellRequested -= HandleIngotSellRequested;
    }

    private void Update()
    {
        if (!_isGamePlay) return;
        CheckInteractionCondition();
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
    // 플레이어의 구역 진입/이탈 처리
    // =========================================================================
    public void OnPlayerEnterZone(StationZoneType zoneType)
    {
        _activeZone = zoneType;
        Debug.Log($"[StationController] 구역 진입: {zoneType}");
    }

    public void OnPlayerExitZone(StationZoneType zoneType)
    {
        if (_activeZone == zoneType)
        {
            _activeZone = null;
            DeactivateInteraction();
        }
    }

    // =========================================================================
    // 상호작용 조건 체크 (Update)
    // =========================================================================
    private void CheckInteractionCondition()
    {
        if (_shipController == null || _activeZone == null)
        {
            if (_isInteracting) DeactivateInteraction();
            return;
        }

        bool speedOK = _shipController.IsInteractable;

        if (speedOK && !_isInteracting)
            ActivateInteraction(_activeZone.Value);
        else if (!speedOK && _isInteracting)
            DeactivateInteraction();
    }

    // =========================================================================
    // 상호작용 활성화 / 비활성화
    // =========================================================================
    private void ActivateInteraction(StationZoneType zone)
    {
        _isInteracting = true;
        GameEvents.RaiseStationInteractionChanged(zone, true);

        StopInteractCoroutine();

        switch (zone)
        {
            case StationZoneType.Left:
                _interactCoroutine = StartCoroutine(UnloadOreRoutine());
                break;
            case StationZoneType.Center:
                break;
            case StationZoneType.Right:
                _interactCoroutine = StartCoroutine(LoadFoodRoutine());
                break;
        }

        Debug.Log($"[StationController] 상호작용 활성화: {zone}");
    }

    private void DeactivateInteraction()
    {
        if (!_isInteracting) return;

        _isInteracting = false;

        StopInteractCoroutine();

        foreach (StationZoneType z in Enum.GetValues(typeof(StationZoneType)))
            GameEvents.RaiseStationInteractionChanged(z, false);

        _shipInventory?.StopTransfer();
        Debug.Log("[StationController] 상호작용 비활성화");
    }

    private void StopInteractCoroutine()
    {
        if (_interactCoroutine != null)
        {
            StopCoroutine(_interactCoroutine);
            _interactCoroutine = null;
        }
    }

    // =========================================================================
    // 적재 코루틴: 식량 -> 우주선
    // =========================================================================
    private IEnumerator LoadFoodRoutine()
    {
        if (_shipInventory == null) yield break;

        int available = Mathf.FloorToInt(StoredFood);
        if (available <= 0)
        {
            Debug.Log("[StationController] 적재할 식량이 없습니다.");
            yield break;
        }

        int canLoad = _shipInventory.Capacity - _shipInventory.Count;
        int toLoad = Mathf.Min(available, canLoad);

        if (toLoad <= 0)
        {
            Debug.Log("[StationController] 우주선 인벤토리가 가득 찼습니다.");
            yield break;
        }

        StoredFood -= toLoad;
        StoredFood = Mathf.Max(0f, StoredFood);
        GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);

        _shipInventory.StartLoading(
            ShipInventory.CargoType.Food,
            toLoad,
            loaded =>
            {
                int refund = toLoad - loaded;
                if (refund > 0)
                {
                    StoredFood += refund;
                    GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);
                }
                Debug.Log($"[StationController] 식량 적재 완료: {loaded}개");
            }
        );

        yield break;
    }

    // =========================================================================
    // 하역 코루틴: 우주선 광석 -> 정거장 저장소
    // =========================================================================
    private IEnumerator UnloadOreRoutine()
    {
        if (_shipInventory == null) yield break;

        if (_shipInventory.CountOf(ShipInventory.CargoType.Ore) <= 0)
        {
            Debug.Log("[StationController] 하역할 광석이 없습니다.");
            yield break;
        }

        _shipInventory.StartUnloading(
            ShipInventory.CargoType.Ore,
            onEach: () =>
            {
                StoredOre += 1f;
                GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);
            },
            onComplete: unloaded =>
            {
                Debug.Log($"[StationController] 광석 하역 완료: {unloaded}개 → 저장소 총 {StoredOre:F0}개");
            }
        );

        yield break;
    }

    // =========================================================================
    // Idle 루프: 농장 + 제련소
    // =========================================================================
    private IEnumerator IdleProductionRoutine()
    {
        while (true)
        {
            float dt = Time.deltaTime;

            StoredFood += _farmRate * dt;

            if (StoredOre >= 1f)
            {
                float refineAmount = Mathf.Min(_refineRate * dt, StoredOre);
                StoredOre -= refineAmount;
                StoredIngot += refineAmount / _oreToIngotRatio;
            }

            GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);

            yield return null;
        }
    }

    // =========================================================================
    // 주괴 판매 (이벤트로 요청받음)
    // =========================================================================
    private void HandleIngotSellRequested()
    {
        if (StoredIngot < 1f)
        {
            Debug.Log("[StationController] 판매할 주괴가 없습니다.");
            return;
        }

        // 주괴 전량 판매 (주괴 1개 = 골드 1개)
        float ingotToSell = Mathf.Floor(StoredIngot);
        float goldEarned = ingotToSell;

        StoredIngot -= ingotToSell;
        GameManager.Instance.AddGold(goldEarned);
        GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);

        Debug.Log($"[StationController] 주괴 {ingotToSell:F0}개 판매 → 골드 +{goldEarned:F0}");
    }


    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleDataInitialized()
    {
        LoadStats();
        CacheShipReferences();
        BroadcastStorage();
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        _isGamePlay = next == GameState.GamePlay;

        if (_isGamePlay)
        {
            ResetTempUpgrades();
            StartIdleProduction();
        }
        else
        {
            StopIdleProduction();
            DeactivateInteraction();
        }
    }

    private void HandleUpgradeCompleted(string upgradeId, int level)
    {
        if (upgradeId == UPGRADE_FARM || upgradeId == UPGRADE_REFINE)
            LoadStats();
    }

    private void HandleOreUnload(float amount)
    {
        StoredOre += amount;
        GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);
    }

    // =========================================================================
    // 스탯 로드
    // =========================================================================
    private void LoadStats()
    {
        var dm = GameDataManager.Instance;
        var gm = GameManager.Instance;

        // 광석/주괴 교환 비율: GameConstant에서 읽음
        _oreToIngotRatio = dm.Get<GameConstantData>(CONST_ORE_TO_INGOT)?.Value
                           ?? dm.Get<UpgradeData>(UPGRADE_FARM)?.BaseStats
                           ?? 10f;

        _farmRate = gm.GetUpgradeStat(UPGRADE_FARM);

        // 제련소: 동일 원칙
        _refineRate = gm.GetUpgradeStat(UPGRADE_REFINE);

        Debug.Log($"[StationController] 스탯 로드 - 농장:{_farmRate:F2}/s, 제련:{_refineRate:F2}/s, 비율:1:{_oreToIngotRatio}");
    }

    // =========================================================================
    // 정거장 임시 업그레이드 초기화 (스테이지 시작 시)
    // =========================================================================
    private void ResetTempUpgrades()
    {
        var context = GameManager.Instance.Context;

        var dm = GameDataManager.Instance;
        var keysToReset = new List<string>();

        foreach (var kvp in context.UpgradeLevels)
        {
            var data = dm.Get<UpgradeData>(kvp.Key);
            if (data != null && data.UpgradeType == "TEMP")
                keysToReset.Add(kvp.Key);
        }

        foreach (var key in keysToReset)
            context.UpgradeLevels[key] = 0;

        if (keysToReset.Count > 0)
        {
            LoadStats();
            Debug.Log($"[StationController] 임시 업그레이드 {keysToReset.Count}개 초기화");
        }
    }

    // =========================================================================
    // 레퍼런스 캐싱 (씬에서 태그로 탐색)
    // =========================================================================
    private void CacheShipReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[StationController] 'Player' 태그 오브젝트를 찾지 못했습니다.");
            return;
        }

        _shipController = player.GetComponent<ShipController>();
        _shipInventory = player.GetComponent<ShipInventory>();

        if (_shipController == null)
            Debug.LogWarning("[StationController] ShipController를 찾지 못했습니다.");
        if (_shipInventory == null)
            Debug.LogWarning("[StationController] ShipInventory를 찾지 못했습니다.");
    }

    // =========================================================================
    // Idle 루프 제어
    // =========================================================================
    private void StartIdleProduction()
    {
        StopIdleProduction();
        _idleCoroutine = StartCoroutine(IdleProductionRoutine());
    }

    private void StopIdleProduction()
    {
        if (_idleCoroutine != null)
        {
            StopCoroutine(_idleCoroutine);
            _idleCoroutine = null;
        }
    }

    // =========================================================================
    // 저장소 브로드캐스트
    // =========================================================================
    private void BroadcastStorage()
        => GameEvents.RaiseStationStorageChanged(StoredFood, StoredOre, StoredIngot);

    // =========================================================================
    // 디버그
    // =========================================================================
#if UNITY_EDITOR
    [ContextMenu("디버그: 식량 100 추가")]
    private void Debug_AddFood() { StoredFood += 100f; BroadcastStorage(); }

    [ContextMenu("디버그: 광석 50 추가")]
    private void Debug_AddOre() { StoredOre += 50f; BroadcastStorage(); }

    [ContextMenu("디버그: 주괴 10 추가")]
    private void Debug_AddIngot() { StoredIngot += 10f; BroadcastStorage(); }

    [ContextMenu("디버그: 주괴 전량 판매")]
    private void Debug_SellIngot() => HandleIngotSellRequested();

    [ContextMenu("디버그: 현재 상태 출력")]
    private void Debug_PrintState()
    {
        Debug.Log($"[StationController] 식량:{StoredFood:F1} | 광석:{StoredOre:F1} | 주괴:{StoredIngot:F2} | 농장:{_farmRate:F2}/s | 제련:{_refineRate:F2}/s");
    }
#endif
}