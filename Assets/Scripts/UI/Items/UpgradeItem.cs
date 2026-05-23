using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GameData;

// 업그레이드 아이템 UI
public class UpgradeItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("아이템")]
    [SerializeField] private TMP_Text _txtLevel;
    [SerializeField] private Image _imgUpgrade;
    [SerializeField] private Button _btnUpgrade;

    [Header("잠금 Alpha")]
    [SerializeField] private float _lockedAlpha = 0.4f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private string _upgradeId;
    private int _slotLevel;
    private StationController _station;
    private CanvasGroup _canvasGroup;
    private UpgradeInfo _upgradeInfo;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_btnUpgrade != null)
            _btnUpgrade.onClick.AddListener(OnClickUpgrade);
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<string, int>(GameEventType.UpgradeCompleted, HandleUpgradeCompleted);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string, int>(GameEventType.UpgradeCompleted, HandleUpgradeCompleted);
        _upgradeInfo?.Hide();
    }

    private void OnDestroy()
    {
        if (_btnUpgrade != null)
            _btnUpgrade.onClick.RemoveListener(OnClickUpgrade);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void SetInfo(UpgradeInfo upgradeInfo)
    {
        _upgradeInfo = upgradeInfo;
        _upgradeInfo?.Hide();
    }

    public void Setup(string upgradeId, int slotLevel, StationController station)
    {
        _upgradeId = upgradeId;
        _slotLevel = slotLevel;
        _station = station;

        _upgradeInfo?.Hide();
        Refresh();
    }

    // =========================================================================
    // 호버 처리
    // =========================================================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_canvasGroup == null || !_canvasGroup.interactable) return;
        _upgradeInfo?.Show(_upgradeId, _slotLevel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _upgradeInfo?.Hide();
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickUpgrade()
    {
        if (_station == null) return;

        bool success = _station.TryStationUpgrade(_upgradeId);
        if (!success)
            Debug.Log($"[UpgradeItem] 업그레이드 실패: {_upgradeId} (재화 부족)");
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleUpgradeCompleted(string upgradeId, int newLevel)
    {
        Refresh();
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void Refresh()
    {
        if (string.IsNullOrEmpty(_upgradeId)) return;

        UpgradeData data = GameDataManager.Instance.Get<UpgradeData>(_upgradeId);
        if (data == null) return;

        int currentLevel = GameManager.Instance.GetUpgradeLevel(_upgradeId);

        // 레벨 표시
        if (_txtLevel != null)
            _txtLevel.text = $"Lv.{_slotLevel}";

        // 스프라이트 로드
        if (_imgUpgrade != null && !string.IsNullOrEmpty(data.SpritePath))
        {
            string sheetName = Path.GetFileName(data.SpritePath);
            string spriteName = $"{sheetName}_{_slotLevel - 1}";
            ResourceManager.Instance.LoadSpriteFromSheet(data.SpritePath, spriteName, OnSpriteLoaded);
        }

        // 해금 여부 - 이전 업그레이드 레벨 체크
        bool isUnlocked = _slotLevel == 1 || currentLevel >= _slotLevel - 1;
        bool isAlreadyDone = currentLevel >= _slotLevel;
        bool isMaxLevel = currentLevel >= data.MaxLevel;

        _canvasGroup.alpha = isUnlocked ? 1f : _lockedAlpha;
        _canvasGroup.interactable = isUnlocked && !isAlreadyDone && !isMaxLevel;
    }

    // =========================================================================
    // 내부 콜백
    // =========================================================================
    private void OnSpriteLoaded(Sprite sprite)
    {
        if (_imgUpgrade != null && sprite != null)
            _imgUpgrade.sprite = sprite;
    }
}