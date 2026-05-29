using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameData;

public class GamePlayUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private FuelGauge _fuelGauge;
    [SerializeField] private CargoInfo _cargoInfo;
    [SerializeField] private AlertBorder _alertBorder;
    [SerializeField] private Button _btnClear;
    // =========================================================================
    // 직접 참조
    // =========================================================================
    private ShipController _controller;
    private ShipInventory _cargo;
    private PlanetController _hoveredPlanet;
    private readonly Dictionary<string, PlanetController> _planetMap = new Dictionary<string, PlanetController>();

    private readonly List<string> _boundPopupIds = new List<string>();

    // =========================================================================
    // 이전 값 캐싱 (변경 시에만 갱신)
    // =========================================================================
    private float _lastFuel = -1f;
    private float _lastMaxFuel = -1f;
    private bool _lastIsBoosting;
    private bool _lastIsOverheat;
    private int _lastCargoCount = -1;
    private int _lastCargoCapacity = -1;

    private int _warningPlanetCount = 0;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void RegisterEvents()
    {
        GameEventBus.Subscribe<Transform>(GameEventType.ShipSpawned, HandleShipSpawned);
        GameEventBus.Subscribe<string, bool>(GameEventType.PlanetHovered, HandlePlanetHovered);
        GameEventBus.Subscribe<Transform>(GameEventType.PlanetSpawned, HandlePlanetSpawned);
        GameEventBus.Subscribe<string>(GameEventType.PlanetDestroyed, HandlePlanetDestroyed);
        GameEventBus.Subscribe<string, bool>(GameEventType.PlanetWarning, HandlePlanetGameOverWarning);
        GameEventBus.Subscribe(GameEventType.StageClearCondition, HandleStageClearCondition);
    }

    protected override void UnregisterEvents()
    {
        GameEventBus.Unsubscribe<Transform>(GameEventType.ShipSpawned, HandleShipSpawned);
        GameEventBus.Unsubscribe<string, bool>(GameEventType.PlanetHovered, HandlePlanetHovered);
        GameEventBus.Unsubscribe<Transform>(GameEventType.PlanetSpawned, HandlePlanetSpawned);
        GameEventBus.Unsubscribe<string>(GameEventType.PlanetDestroyed, HandlePlanetDestroyed);
        GameEventBus.Unsubscribe<string, bool>(GameEventType.PlanetWarning, HandlePlanetGameOverWarning);
        GameEventBus.Unsubscribe(GameEventType.StageClearCondition, HandleStageClearCondition);
    }

    protected override void Start()
    {
        base.Start();
        _fuelGauge.Initialize();
        _cargoInfo.Initialize();

        if (_btnClear != null)
        {
            _btnClear.gameObject.SetActive(false);
        }

        CollectBoundPopups();
    }

    private void Update()
    {
        if (_controller == null || _cargo == null) return;

        // 연료
        if (!Mathf.Approximately(_controller.CurrentFuel, _lastFuel) ||
            !Mathf.Approximately(_controller.MaxFuel, _lastMaxFuel))
        {
            _lastFuel = _controller.CurrentFuel;
            _lastMaxFuel = _controller.MaxFuel;
            _fuelGauge.RefreshFuel(_lastFuel, _lastMaxFuel);
        }

        // 부스터
        if (_controller.IsBoosting != _lastIsBoosting)
        {
            _lastIsBoosting = _controller.IsBoosting;
            _fuelGauge.RefreshBooster(_lastIsBoosting);
        }

        // 과열
        if (_controller.IsOverheat != _lastIsOverheat)
        {
            _lastIsOverheat = _controller.IsOverheat;
            if (_lastIsOverheat)
                _fuelGauge.RefreshBooster(false);
        }

        // 화물
        if (_cargo.Count != _lastCargoCount ||
            _cargo.Capacity != _lastCargoCapacity)
        {
            _lastCargoCount = _cargo.Count;
            _lastCargoCapacity = _cargo.Capacity;
            _cargoInfo.RefreshCargo(
                _cargo.CountOf(ShipInventory.CargoType.Food),
                _cargo.CountOf(ShipInventory.CargoType.Ore),
                _lastCargoCount,
                _lastCargoCapacity
            );
        }
    }

    public override void Open()
    {
        base.Open();

        if (_btnClear != null)
        {
            _btnClear.onClick.RemoveListener(OnClickClear);
            _btnClear.onClick.AddListener(OnClickClear);
            _btnClear.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleShipSpawned(Transform shipTransform)
    {
        _controller = shipTransform.GetComponent<ShipController>();
        _cargo = shipTransform.GetComponent<ShipInventory>();

        _lastFuel = -1f;
        _lastMaxFuel = -1f;
        _lastCargoCount = -1;
        _lastCargoCapacity = -1;
    }

    private void HandlePlanetSpawned(Transform planetTransform)
    {
        PlanetController planet = planetTransform.GetComponent<PlanetController>();
        if (planet == null) return;

        _planetMap[planet.InstanceId] = planet;
    }

    private void HandlePlanetDestroyed(string instanceId)
    {
        if (_hoveredPlanet != null && _hoveredPlanet.InstanceId == instanceId)
        {
            UIManager.Instance.CloseUI(UIId.Popup.PlanetInfo);
            _hoveredPlanet = null;
        }

        _planetMap.Remove(instanceId);

        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        GameEventBus.Publish(GameEventType.StageFailed, stageId);
    }

    private void HandlePlanetHovered(string instanceId, bool isHover)
    {
        if (!isHover)
        {
            UIManager.Instance.CloseUI(UIId.Popup.PlanetInfo);
            _hoveredPlanet = null;
            return;
        }

        PlanetController planet = FindPlanetById(instanceId);
        if (planet == null) return;

        _hoveredPlanet = planet;

        PlanetInfo planetInfo = UIManager.Instance.PrepareUI<PlanetInfo>(UIId.Popup.PlanetInfo);
        if (planetInfo != null)
        {
            planetInfo.Setup(_hoveredPlanet);
            UIManager.Instance.OpenUI(UIId.Popup.PlanetInfo);
        }
    }

    private void HandlePlanetGameOverWarning(string instanceId, bool isWarning)
    {
        _warningPlanetCount += isWarning ? 1 : -1;
        _warningPlanetCount = Mathf.Max(0, _warningPlanetCount);

        if (_warningPlanetCount > 0)
            _alertBorder.StartAlert();
        else
            _alertBorder.StopAlert();
    }

    private void HandleStageClearCondition()
    {
        if (_btnClear != null)
            _btnClear.gameObject.SetActive(true);
    }

    private void OnClickClear()
    {
        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        if (string.IsNullOrEmpty(stageId)) return;

        StageManager.Instance.TryClearStage(stageId);
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private PlanetController FindPlanetById(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;

        if (_planetMap.TryGetValue(instanceId, out PlanetController planet))
            return planet;

        return null;
    }

    private void CollectBoundPopups()
    {
        _boundPopupIds.Clear();

        foreach (UIData data in GameDataManager.Instance.GetAll<UIData>())
        {
            if (data.BindState != "GamePlay") continue;
            if (data.Id == UiId) continue;

            _boundPopupIds.Add(data.Id);
        }
    }

    // =========================================================================
    // 닫기 전 정리
    // =========================================================================
    protected override void OnBeforeClose()
    {
        _planetMap.Clear();
        _hoveredPlanet = null;
        _warningPlanetCount = 0;
        _alertBorder.StopAlert();

        if (_btnClear != null)
        {
            _btnClear.onClick.RemoveListener(OnClickClear);
            _btnClear.gameObject.SetActive(false);
        }

        foreach (string popupId in _boundPopupIds)
            UIManager.Instance.CloseUI(popupId);
    }
}