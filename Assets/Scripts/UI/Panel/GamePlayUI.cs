using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class GamePlayUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================

    [Header("연료 슬라이더")]
    [SerializeField] private Slider _fuelSlider;

    [Header("부스터 아이콘 (img_boost)")]
    [SerializeField] private Image _boostIcon;
    [SerializeField] private Sprite _boostOnSprite;
    [SerializeField] private Sprite _boostOffSprite;

    [Header("재화")]
    [SerializeField] private TMP_Text _txtGold;
    [SerializeField] private TMP_Text _txtIngot;

    [Header("화물")]
    [SerializeField] private TMP_Text _txtCargoInfo;
    [SerializeField] private TMP_Text _txtFood;
    [SerializeField] private TMP_Text _txtOre;
    [SerializeField] private Button _btnCargo;
    [SerializeField] private GameObject _cargoPanel;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    private void OnEnable()
    {
        GameEvents.OnFuelChanged += HandleFuelChanged;
        GameEvents.OnBoosterChanged += HandleBoosterChanged;
        GameEvents.OnOverheatChanged += HandleOverheatChanged;
        GameEvents.OnGoldChanged += HandleGoldChanged;
        GameEvents.OnIngotChanged += HandleIngotChanged;
        GameEvents.OnCargoDetailChanged += HandleCargoDetailChanged;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnDataInitialized += HandleDataInitialized;
        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInitialized)
            HandleDataInitialized();
    }

    private void OnDisable()
    {
        GameEvents.OnFuelChanged -= HandleFuelChanged;
        GameEvents.OnBoosterChanged -= HandleBoosterChanged;
        GameEvents.OnOverheatChanged -= HandleOverheatChanged;
        GameEvents.OnGoldChanged -= HandleGoldChanged;
        GameEvents.OnIngotChanged -= HandleIngotChanged;
        GameEvents.OnCargoDetailChanged -= HandleCargoDetailChanged;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnDataInitialized -= HandleDataInitialized;
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
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================

    private void HandleFuelChanged(float current, float max)
        => RefreshFuel(current, max);

    private void HandleBoosterChanged(bool isOn)
        => RefreshBooster(isOn);

    private void HandleOverheatChanged(bool isOverheat)
    {
        if (isOverheat)
            RefreshBooster(false);
    }

    private void HandleGoldChanged(float gold) => RefreshGold(gold);
    private void HandleIngotChanged(float ingot) => RefreshIngot(ingot);
    private void HandleCargoDetailChanged(int food, int ore, int total, int capacity)  => RefreshCargo(food, ore, total, capacity);

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (next != GameState.GamePlay) return;

        var ctx = GameManager.Instance.Context;
        RefreshGold(ctx.CurrentGold);
        RefreshIngot(ctx.CurrentIngot);
    }

    private void HandleDataInitialized()
    {
        var ctx = GameManager.Instance.Context;
        RefreshGold(ctx.CurrentGold);
        RefreshIngot(ctx.CurrentIngot);
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

    private void RefreshGold(float gold)
    {
        if (_txtGold == null) return;
        _txtGold.text = Mathf.FloorToInt(gold).ToString("N0");
    }

    private void RefreshIngot(float ingot)
    {
        if (_txtIngot == null) return;
        _txtIngot.text = Mathf.FloorToInt(ingot).ToString("N0");
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