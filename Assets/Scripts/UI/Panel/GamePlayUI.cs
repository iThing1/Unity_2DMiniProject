using GameData;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("연료 슬라이더")]
    [SerializeField] private Slider _fuelSlider;

    [Header("부스터 아이콘")]
    [SerializeField] private Image _boostIcon;
    [SerializeField] private Sprite _boostOnSprite;
    [SerializeField] private Sprite _boostOffSprite;

    [Header("화물")]
    [SerializeField] private TMP_Text _txtCargoInfo;
    [SerializeField] private TMP_Text _txtFood;
    [SerializeField] private TMP_Text _txtOre;
    [SerializeField] private Button _btnCargo;
    [SerializeField] private GameObject _cargoPanel;

    [Header("행성 정보 팝업")]
    [SerializeField] private GameObject _planetInfoPrefab;

    [Header("게임오버 경고")] // TODO: 나중에 9-Slice로 바꿈
    [SerializeField] private GameObject _alertBorder;
    [SerializeField] private float _alertBlinkInterval = 0.4f;
    // =========================================================================
    // 직접 참조
    // =========================================================================
    private ShipController _controller;
    private ShipInventory _cargo;
    private PlanetInfo _planetInfo;
    private PlanetController _hoveredPlanet;
    private readonly Dictionary<string, PlanetController> _planetMap = new Dictionary<string, PlanetController>();

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
    private Coroutine _blinkCoroutine;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnShipSpawned += HandleShipSpawned;
        GameEvents.OnPlanetHovered += HandlePlanetHovered;
        GameEvents.OnPlanetSpawned += HandlePlanetSpawned;
        GameEvents.OnPlanetDestroyed += HandlePlanetDestroyed;
        GameEvents.OnPlanetGameOverWarning += HandlePlanetGameOverWarning;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnShipSpawned -= HandleShipSpawned;
        GameEvents.OnPlanetHovered -= HandlePlanetHovered;
        GameEvents.OnPlanetSpawned -= HandlePlanetSpawned;
        GameEvents.OnPlanetDestroyed -= HandlePlanetDestroyed;
        GameEvents.OnPlanetGameOverWarning -= HandlePlanetGameOverWarning;
    }

    private void OnDestroy()
    {
        if (_btnCargo != null)
            _btnCargo.onClick.RemoveListener(OnClickCargo);
    }

    protected override void Start()
    {
        base.Start();
        RefreshFuel(0f, 1f);
        RefreshBooster(false);
        RefreshCargo(0, 0, 0, 1);

        if (_btnCargo != null)
            _btnCargo.onClick.AddListener(OnClickCargo);

        SetCargoPanel(false);
        SpawnPlanetInfo();

        if (_alertBorder != null)
            _alertBorder.SetActive(false);
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
            RefreshFuel(_lastFuel, _lastMaxFuel);
        }

        // 부스터
        if (_controller.IsBoosting != _lastIsBoosting)
        {
            _lastIsBoosting = _controller.IsBoosting;
            RefreshBooster(_lastIsBoosting);
        }

        // 과열
        if (_controller.IsOverheat != _lastIsOverheat)
        {
            _lastIsOverheat = _controller.IsOverheat;
            if (_lastIsOverheat)
                RefreshBooster(false);
        }

        // 화물
        if (_cargo.Count != _lastCargoCount ||
            _cargo.Capacity != _lastCargoCapacity)
        {
            _lastCargoCount = _cargo.Count;
            _lastCargoCapacity = _cargo.Capacity;
            RefreshCargo(_cargo.CountOf(ShipInventory.CargoType.Food), _cargo.CountOf(ShipInventory.CargoType.Ore), _lastCargoCount, _lastCargoCapacity);
        }
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (next == GameState.GamePlay) return;

        _planetMap.Clear();
        _planetInfo?.Hide();
        _hoveredPlanet = null;
        _warningPlanetCount = 0;

        StopAlert();
    }

    private void HandleShipSpawned(Transform shipTransform)
    {
        _controller = shipTransform.GetComponent<ShipController>();
        _cargo = shipTransform.GetComponent<ShipInventory>();

        // 캐시 초기화 (다음 Update에서 즉시 갱신)
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
            _planetInfo?.Hide();
            _hoveredPlanet = null;
        }

        _planetMap.Remove(instanceId);

        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        GameEvents.RaiseStageFailed(stageId);
    }

    private void HandlePlanetHovered(string instanceId, bool isHover)
    {
        if (!isHover)
        {
            _planetInfo?.Hide();
            _hoveredPlanet = null;
            return;
        }

        PlanetController planet = FindPlanetById(instanceId);
        if (planet == null) return;

        _hoveredPlanet = planet;
        _planetInfo?.Show(_hoveredPlanet);
    }

    private void HandlePlanetGameOverWarning(string instanceId, bool isWarning)
    {
        _warningPlanetCount += isWarning ? 1 : -1;
        _warningPlanetCount = Mathf.Max(0, _warningPlanetCount);

        if (_warningPlanetCount > 0)
            StartAlert();
        else
            StopAlert();
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickCargo()
    {
        if (_cargoPanel == null) return;
        SetCargoPanel(!_cargoPanel.activeSelf);
    }

    private void SetCargoPanel(bool isOpen)
    {
        if (_cargoPanel != null)
            _cargoPanel.SetActive(isOpen);
    }

    private void SpawnPlanetInfo()
    {
        if (_planetInfoPrefab == null)
        {
            Debug.LogWarning("[GamePlayUI] PlanetInfoPrefab이 연결되지 않았습니다.");
            return;
        }

        GameObject instance = Instantiate(_planetInfoPrefab, transform);
        _planetInfo = instance.GetComponent<PlanetInfo>();

        if (_planetInfo == null)
        {
            Debug.LogError("[GamePlayUI] PlanetInfoPopup 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        _planetInfo.Hide();
    }

    private PlanetController FindPlanetById(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;

        if (_planetMap.TryGetValue(instanceId, out PlanetController planet))
        {
            return planet;
        }

        Debug.LogWarning($"[GamePlayUI] 딕셔너리에서 행성을 찾지 못했습니다: {instanceId}");
        return null;
    }

    // =========================================================================
    // 게임오버 경고 점멸
    // =========================================================================
    private void StartAlert()
    {
        if (_alertBorder == null) return;
        if (_blinkCoroutine != null) return;

        _alertBorder.SetActive(true);
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopAlert()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        if (_alertBorder != null)
            _alertBorder.SetActive(false);
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            _alertBorder.SetActive(true);
            yield return new WaitForSeconds(_alertBlinkInterval);
            _alertBorder.SetActive(false);
            yield return new WaitForSeconds(_alertBlinkInterval);
        }
    }


    // =========================================================================
    // UI 갱신 메서드
    // =========================================================================
    private void RefreshFuel(float current, float max)
    {
        if (_fuelSlider == null) return;
        _fuelSlider.value = max > 0f ? current / max : 0f;
    }

    private void RefreshBooster(bool isOn)
    {
        if (_boostIcon == null) return;
        Sprite target = isOn ? _boostOnSprite : _boostOffSprite;
        if (target != null)
            _boostIcon.sprite = target;
    }

    private void RefreshCargo(int food, int ore, int total, int capacity)
    {
        if (_txtCargoInfo != null)
            _txtCargoInfo.text = $"{total} / {capacity}";

        if (_txtFood != null)
            _txtFood.text = food.ToString();

        if (_txtOre != null)
            _txtOre.text = ore.ToString();
    }
}