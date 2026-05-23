using System.Collections;
using UnityEngine;
using GameData;

// 행성 오브젝트의 진입점
[RequireComponent(typeof(PlanetSimulator))]
public class PlanetController : InteractableBase
{
    // =========================================================================
    // 런타임 상태
    // =========================================================================
    public string InstanceId { get; private set; }
    public string PlanetName { get; private set; }
    public string PlanetSprite { get; private set; }
    public PlanetSize Size { get; private set; }

    public float Prosperity => _simulator.Prosperity;
    public float Population => _simulator.Population;
    public float StoredFood => _simulator.StoredFood;
    public float StoredOre => _simulator.StoredOre;
    public float CycleProgress => _simulator.CycleProgress;
    public PlanetState State => _simulator.State;
    public bool IsGameOverWarning => _simulator.IsGameOverWarning;

    // =========================================================================
    // 내부
    // =========================================================================
    private PlanetSimulator _simulator;
    private Coroutine _interactCoroutine;
    private PlanetProgressBar _progressBar;
    private PlanetWarningIndicator _warningIndicator;
    private SpriteRenderer _spriteRenderer;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _simulator = GetComponent<PlanetSimulator>();
        _progressBar = GetComponentInChildren<PlanetProgressBar>();
        _warningIndicator = GetComponentInChildren<PlanetWarningIndicator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
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
        PlanetSprite = data.PlanetSprite;
        Size = data.Size;

        _simulator.Initialize(data, instanceId);
        _progressBar.Initialize(_simulator);

        if (_warningIndicator != null)
            _warningIndicator.Initialize(instanceId);

        string[] parts = data.PlanetSprite.Split('/');
        if (parts.Length == 2)
            ResourceManager.Instance.LoadSpriteFromSheet(parts[0], parts[1], OnSpriteLoaded);
        else
            Debug.LogWarning($"[PlanetController] PlanetSprite 경로 형식이 올바르지 않습니다: {data.PlanetSprite}");
    }

    private void OnSpriteLoaded(Sprite sprite)
    {
        if (_spriteRenderer == null) return;
        if (sprite == null)
        {
            Debug.LogWarning($"[PlanetController] 스프라이트 로드 실패: {PlanetSprite}");
            return;
        }

        _spriteRenderer.sprite = sprite;
    }

    // =========================================================================
    // 마우스 오버 (추가)
    // =========================================================================
    private void OnMouseEnter()
    {
        GameEventBus.Publish(GameEventType.PlanetHovered, true);
    }

    private void OnMouseExit()
    {
        GameEventBus.Publish(GameEventType.PlanetHovered, false);
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
            _shipInventory.StartUnloading(ShipInventory.CargoType.Food, OnFoodUnloadEach, null);
        }

        int oreToLoad = Mathf.Min(
            Mathf.FloorToInt(_simulator.StoredOre),
            _shipInventory.Capacity - _shipInventory.Count
        );
        if (oreToLoad > 0)
        {
            _shipInventory.StartLoading(ShipInventory.CargoType.Ore, oreToLoad, OnOreLoadEach, null);
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
        _simulator.DeliverFood(1);
    }

    private void OnOreLoadEach()
    {
        _simulator.CollectOre(1);
    }

    private bool IsLoadingDone()
    {
        return !_shipInventory.IsLoading;
    }
}