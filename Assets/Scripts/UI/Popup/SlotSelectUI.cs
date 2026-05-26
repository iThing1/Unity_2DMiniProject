using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public enum SlotSelectMode { NewGame, Continue }

public class SlotSelectUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private Transform _slotContent;
    [SerializeField] private GameObject _slotItemPrefab;
    [SerializeField] private Button _btnStart;
    [SerializeField] private TextMeshProUGUI _txtTitle;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly List<SaveSlotItem> _slotItems = new List<SaveSlotItem>();
    private int _selectedSlotIndex = -1;
    private SlotSelectMode _mode;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        if (_btnStart != null)
            _btnStart.onClick.AddListener(OnClickStart);

        SetStartButtonActive(false);
    }

    private void OnDestroy()
    {
        if (_btnStart != null)
            _btnStart.onClick.RemoveListener(OnClickStart);
    }

    // =========================================================================
    // 목록 세팅
    // =========================================================================
    public override void Setup(object data = null)
    {
        base.Setup(data);

        if (data is SlotSelectMode mode)
        {
            _mode = mode;

            if (_txtTitle != null)
                _txtTitle.text = mode == SlotSelectMode.NewGame ? "New Game" : "Continue";

            _selectedSlotIndex = -1;
            SetStartButtonActive(false);
            RefreshSlots();
        }
    }

    // =========================================================================
    // 슬롯 목록 생성
    // =========================================================================
    private void RefreshSlots()
    {
        foreach (SaveSlotItem item in _slotItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _slotItems.Clear();

        if (_slotItemPrefab == null || _slotContent == null) return;

        for (int i = 0; i < SaveLoadController.MAX_SLOTS; i++)
        {
            GameContext context = SaveLoadController.PeekSlot(i);

            GameObject instance = Instantiate(_slotItemPrefab, _slotContent);
            SaveSlotItem item = instance.GetComponent<SaveSlotItem>();
            if (item == null) continue;

            int slotIndex = i;
            item.Setup(slotIndex, context, OnSlotSelected);
            _slotItems.Add(item);
        }
    }

    // =========================================================================
    // 슬롯 선택
    // =========================================================================
    private void OnSlotSelected(int slotIndex)
    {
        _selectedSlotIndex = slotIndex;

        for (int i = 0; i < _slotItems.Count; i++)
            _slotItems[i].SetSelected(i == slotIndex);

        bool canStart = _mode == SlotSelectMode.NewGame || !_slotItems[slotIndex].IsEmpty();
        SetStartButtonActive(canStart);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickStart()
    {
        if (_selectedSlotIndex < 0) return;

        switch (_mode)
        {
            case SlotSelectMode.NewGame:
                if (SaveLoadController.HasSlot(_selectedSlotIndex))
                {
                    Confirm confirm = UIManager.Instance.PrepareUI<Confirm>(UIId.Popup.Confirm);
                    if (confirm != null)
                    {
                        confirm.Setup(new ConfirmData("기존 데이터가 삭제됩니다.\n계속하시겠습니까?", OnConfirmNewGame));
                        UIManager.Instance.OpenUI(UIId.Popup.Confirm);
                    }
                }
                else
                {
                    StartNewGame();
                }
                break;
            case SlotSelectMode.Continue:
                GameManager.Instance.LoadGame(_selectedSlotIndex);
                GameEventBus.Publish(GameEventType.ContinueRequested);
                GameManager.Instance.ChangeState(GameState.Lobby);
                Close();
                break;
        }
    }

    private void OnConfirmNewGame()
    {
        Close();
        StartNewGame();
    }

    private void StartNewGame()
    {
        SaveLoadController.DeleteSlot(_selectedSlotIndex);
        GameManager.Instance.ResetContext(_selectedSlotIndex);
        GameEventBus.Publish(GameEventType.NewGameRequested);
        GameManager.Instance.ChangeState(GameState.Lobby);
    }


    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private void SetStartButtonActive(bool active)
    {
        if (_btnStart != null)
            _btnStart.interactable = active;
    }
}