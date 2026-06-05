using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotItem : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TextMeshProUGUI _txtDate;
    [SerializeField] private TextMeshProUGUI _txtGold;
    [SerializeField] private TextMeshProUGUI _txtIngot;
    [SerializeField] private TextMeshProUGUI _txtLastStage;
    [SerializeField] private Button _btnSlot;
    [SerializeField] private Image _imgBackground;

    [Header("Alpha Setting")]
    [SerializeField] private float _selectedAlpha = 1f;
    [SerializeField] private float _deselectedAlpha = 0.5f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private int _slotIndex;
    private bool _isEmpty;
    private System.Action<int> _onSelected;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (_btnSlot != null)
            _btnSlot.onClick.AddListener(OnClickSlot);
    }

    private void OnDestroy()
    {
        if (_btnSlot != null)
            _btnSlot.onClick.RemoveListener(OnClickSlot);
    }

    public void Setup(int slotIndex, GameContext context, System.Action<int> onSelected)
    {
        _slotIndex = slotIndex;
        _onSelected = onSelected;
        _isEmpty = context == null;

        if (_isEmpty)
        {
            SetEmptySlot();
        }
        else
        {
            if (_txtDate != null)
                _txtDate.text = context.SavedAt;

            if (_txtGold != null)
                _txtGold.text = context.CurrentGold.ToString("N0");

            if (_txtIngot != null)
                _txtIngot.text = context.CurrentIngot.ToString("N0");

            if (_txtLastStage != null)
            {
                StageData stageData = GameDataManager.Instance.Get<StageData>(context.LastSelectedStageId);
                _txtLastStage.text = stageData != null ? stageData.Name : "-";
            }
        }

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (_imgBackground == null) return;

        Color color = _imgBackground.color;
        color.a = isSelected ? _selectedAlpha : _deselectedAlpha;
        _imgBackground.color = color;
    }

    public bool IsEmpty()
    {
        return _isEmpty;
    }

    private void SetEmptySlot()
    {
        if (_txtDate != null) _txtDate.text = "-";
        if (_txtGold != null) _txtGold.text = "-";
        if (_txtIngot != null) _txtIngot.text = "-";
        if (_txtLastStage != null) _txtLastStage.text = "빈 슬롯";
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickSlot()
    {
        _onSelected?.Invoke(_slotIndex);
    }
}
