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
    }

    // =========================================================================
    // InteractableBase 구현
    // =========================================================================
    protected override bool CanInteract()
    {
        return _simulator != null && _simulator.IsRunning;
    }

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

    private void OnFoodUnloadComplete(int n) { }

    private void OnOreLoadComplete(int loaded)
    {
        GameEvents.RaiseOreCollected(InstanceId, loaded);
    }

    private bool IsLoadingDone()
    {
        return !_shipInventory.IsLoading;
    }
}