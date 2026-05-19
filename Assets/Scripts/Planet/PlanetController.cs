using System.Collections;
using UnityEngine;
using GameData;

// 행성 오브젝트의 진입점
[RequireComponent(typeof(PlanetSimulator))]
public class PlanetController : InteractableBase
{
    // =========================================================================
    // 런타임 상태 (외부 읽기용 - 시뮬레이터 위임)
    // =========================================================================
    public string InstanceId { get; private set; }
    public string PlanetName { get; private set; }
    public PlanetSize Size { get; private set; }

    public float Prosperity => _simulator.Prosperity;
    public float Population => _simulator.Population;
    public float StoredFood => _simulator.StoredFood;
    public float StoredOre => _simulator.StoredOre;
    public PlanetState State => _simulator.State;
    public bool IsGameOverWarning => _simulator.IsGameOverWarning;

    // =========================================================================
    // 내부
    // =========================================================================
    private PlanetSimulator _simulator;
    private Coroutine _interactCoroutine;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _simulator = GetComponent<PlanetSimulator>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    // =========================================================================
    // 외부 API: 초기화
    // =========================================================================
    public void Initialize(PlanetData data, string instanceId)
    {
        InstanceId = instanceId;
        PlanetName = data.Name;
        Size = data.Size;

        _simulator.Initialize(data, instanceId);

        Debug.Log($"[PlanetController] '{PlanetName}' 초기화 완료");
    }

    // =========================================================================
    // InteractableBase 구현
    // =========================================================================
    protected override bool CanInteract() => _simulator != null && _simulator.IsRunning;

    protected override void OnActivate()
    {
        if (_interactCoroutine != null)
            StopCoroutine(_interactCoroutine);

        _interactCoroutine = StartCoroutine(InteractRoutine());
    }

    protected override void OnDeactivate()
    {
        if (_interactCoroutine != null)
        {
            StopCoroutine(_interactCoroutine);
            _interactCoroutine = null;
        }

        _shipInventory?.StopTransfer();
    }

    // =========================================================================
    // 상호작용 코루틴: 식량 하역 + 광석 적재 동시 진행
    // =========================================================================
    private IEnumerator InteractRoutine()
    {
        int foodCount = _shipInventory.CountOf(ShipInventory.CargoType.Food);
        if (foodCount > 0)
        {
            _shipInventory.StartUnloading(ShipInventory.CargoType.Food, OnFoodUnloadEach, OnFoodUnloadComplete);
        }

        int oreToLoad = Mathf.Min(
            Mathf.FloorToInt(_simulator.StoredOre),
            _shipInventory.Capacity - _shipInventory.Count
        );
        if (oreToLoad > 0)
        {
            _shipInventory.StartLoading(ShipInventory.CargoType.Ore, oreToLoad, OnOreLoadComplete);
        }

        yield return new WaitUntil(IsLoadingDone);

        _interactCoroutine = null;
        CompleteInteraction();
    }

    // =========================================================================
    // 적재/하역 콜백 메서드
    // =========================================================================
    private void OnFoodUnloadEach()
    {
        GameEvents.RaiseFoodDelivered(InstanceId, 1);
    }

    private void OnFoodUnloadComplete(int n)
    {
        Debug.Log($"[PlanetController] '{PlanetName}' 식량 하역 완료: {n}개");
    }

    private void OnOreLoadComplete(int loaded)
    {
        GameEvents.RaiseOreCollected(InstanceId, loaded);
        Debug.Log($"[PlanetController] '{PlanetName}' 광석 적재 완료: {loaded}개");
    }

    private bool IsLoadingDone()
    {
        return !_shipInventory.IsLoading;
    }

    // =========================================================================
    // 디버그
    // =========================================================================
#if UNITY_EDITOR
    [Header("디버그 전용")]
    [SerializeField] private string _debugPlanetId = "Planet_small_01";

    private void Start()
    {
        if (!GameDataManager.Instance.IsInitialized)
        {
            GameEvents.OnDataInitialized += DebugInitialize;
            return;
        }
        DebugInitialize();
    }

    [ContextMenu("디버그: 데이터 초기화")]
    private void DebugInitialize()
    {
        GameEvents.OnDataInitialized -= DebugInitialize;
        var data = GameDataManager.Instance.Get<PlanetData>(_debugPlanetId);
        if (data == null)
        {
            Debug.LogWarning($"[PlanetController] 디버그 데이터 없음: {_debugPlanetId}");
            return;
        }
        Initialize(data, gameObject.name);
    }

    [ContextMenu("디버그: 현재 상태 출력")]
    private void Debug_PrintState()
    {
        Debug.Log($"[{PlanetName}] 번영도:{Prosperity:F1} / 인구:{Population:F0} / 식량:{StoredFood:F0} / 광석:{StoredOre:F0} / 상태:{State}");
    }
#endif
}