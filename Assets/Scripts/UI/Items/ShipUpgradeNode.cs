using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using GameData;

// 우주선 업그레이드 트리 노드
// 나중에 분기 트리 확장 시 Prev → List<ShipUpgradeNode> _prevNodes 로 변경
public class ShipUpgradeNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private Image _imgNode;
    [SerializeField] private Image _imgFrame;
    [SerializeField] private TMP_Text _txtLevel;
    [SerializeField] private Button _btnNode;

    [Header("상태 색상")]
    [SerializeField] private Color _colorUnlocked = Color.white;
    [SerializeField] private Color _colorLocked = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _colorDone = new Color(0.2f, 0.9f, 0.2f, 1f);

    // =========================================================================
    // 링크드리스트
    // =========================================================================
    public ShipUpgradeNode Next { get; private set; }
    public ShipUpgradeNode Prev { get; private set; }

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private string _upgradeId;
    private int _slotLevel;
    private UpgradeInfo _upgradeInfo;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (_btnNode != null)
            _btnNode.onClick.AddListener(OnClickNode);
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
        if (_btnNode != null)
            _btnNode.onClick.RemoveListener(OnClickNode);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void SetInfo(UpgradeInfo upgradeInfo)
    {
        _upgradeInfo = upgradeInfo;
        _upgradeInfo?.Hide();
    }

    public void Setup(string upgradeId, int slotLevel)
    {
        _upgradeId = upgradeId;
        _slotLevel = slotLevel;

        _upgradeInfo?.Hide();
        Refresh();
    }

    public void SetNext(ShipUpgradeNode next)
    {
        Next = next;
        if (next != null)
            next.Prev = this;
    }

    // =========================================================================
    // 해금 여부
    // Prev가 없으면 헤드 노드 → 항상 해금
    // Prev가 있으면 Prev 노드가 완료(Done) 상태일 때만 해금
    // =========================================================================
    public bool IsUnlocked()
    {
        if (Prev == null) return true;
        return Prev.IsDone();
    }

    public bool IsDone()
    {
        int currentLevel = GameManager.Instance.GetUpgradeLevel(_upgradeId);
        return currentLevel >= _slotLevel;
    }

    // =========================================================================
    // 호버 처리
    // =========================================================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsUnlocked() || IsDone()) return;
        _upgradeInfo?.Show(_upgradeId, _slotLevel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _upgradeInfo?.Hide();
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickNode()
    {
        bool success = GameManager.Instance.TryUpgrade(_upgradeId);
        if (!success)
        {
            Debug.Log($"[ShipUpgradeNode] 업그레이드 실패: {_upgradeId} Lv.{_slotLevel} (재화 부족)");
            return;
        }

        SoundManager.Instance.PlaySFX("Sounds/SFX/Success");
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

        bool isUnlocked = IsUnlocked();
        bool isAlreadyDone = IsDone();

        // 레벨 표시
        if (_txtLevel != null)
            _txtLevel.text = $"{_slotLevel}";

        // 스프라이트 로드
        if (_imgNode != null && !string.IsNullOrEmpty(data.SpritePath))
        {
            string sheetName = Path.GetFileName(data.SpritePath);
            string spriteName = $"{sheetName}_{_slotLevel - 1}";
            ResourceManager.Instance.LoadSpriteFromSheet(data.SpritePath, spriteName, OnSpriteLoaded);
        }

        // 상태별 프레임 색상
        if (_imgFrame != null)
        {
            if (isAlreadyDone)
                _imgFrame.color = _colorDone;
            else if (isUnlocked)
                _imgFrame.color = _colorUnlocked;
            else
                _imgFrame.color = _colorLocked;
        }

        // 버튼 활성 여부
        if (_btnNode != null)
            _btnNode.interactable = isUnlocked && !isAlreadyDone;
    }

    // =========================================================================
    // 내부 콜백
    // =========================================================================
    private void OnSpriteLoaded(Sprite sprite)
    {
        if (_imgNode != null && sprite != null)
            _imgNode.sprite = sprite;
    }
}