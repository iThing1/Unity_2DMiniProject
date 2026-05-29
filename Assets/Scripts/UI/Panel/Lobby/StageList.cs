using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class StageList : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TMP_Text _txtStageName;
    [SerializeField] private Button _btnStart;
    [SerializeField] private Button _btnLeft;
    [SerializeField] private Button _btnRight;

    [Header("잠금 표시")]
    [SerializeField] private float _lockedAlpha = 0.3f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private List<StageData> _stageList = new List<StageData>();
    private int _currentIndex = 0;

    // =========================================================================
    // 외부 콜백
    // =========================================================================
    public Action<StageData> OnStageChanged;

    // =========================================================================
    // 외부 읽기용
    // =========================================================================
    public StageData CurrentStage => _stageList.Count > 0 ? _stageList[_currentIndex] : null;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Start()
    {
        if (_btnStart != null)
            _btnStart.onClick.AddListener(OnClickStart);
        if (_btnLeft != null)
            _btnLeft.onClick.AddListener(OnClickLeft);
        if (_btnRight != null)
            _btnRight.onClick.AddListener(OnClickRight);
    }

    private void OnDestroy()
    {
        if (_btnStart != null)
            _btnStart.onClick.RemoveListener(OnClickStart);
        if (_btnLeft != null)
            _btnLeft.onClick.RemoveListener(OnClickLeft);
        if (_btnRight != null)
            _btnRight.onClick.RemoveListener(OnClickRight);
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    public void Initialize()
    {
        LoadStageList();
        RefreshAll();
    }

    // =========================================================================
    // 스테이지 로드
    // =========================================================================
    private void LoadStageList()
    {
        _stageList.Clear();

        foreach (StageData stage in GameDataManager.Instance.GetAll<StageData>())
            _stageList.Add(stage);

        _stageList.Sort(SortById);
        _currentIndex = 0;
    }

    private int SortById(StageData a, StageData b)
    {
        return string.Compare(a.Id, b.Id);
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshAll()
    {
        if (_stageList.Count == 0) return;

        StageData current = _stageList[_currentIndex];
        RefreshStageName(current);
        RefreshStartButton();
        RefreshNavButtons();
        OnStageChanged?.Invoke(current);
    }

    private void RefreshStageName(StageData data)
    {
        if (_txtStageName != null)
            _txtStageName.text = data.Name;
    }

    private void RefreshStartButton()
    {
        if (_btnStart == null) return;
        if (_stageList.Count == 0) return;

        bool isUnlocked = IsStageUnlocked(_currentIndex);

        Image btnImage = _btnStart.GetComponent<Image>();
        if (btnImage != null)
        {
            Color color = btnImage.color;
            color.a = isUnlocked ? 1f : _lockedAlpha;
            btnImage.color = color;
        }

        _btnStart.interactable = isUnlocked;
    }

    private void RefreshNavButtons()
    {
        if (_btnLeft != null)
            _btnLeft.interactable = _currentIndex > 0;

        if (_btnRight != null)
            _btnRight.interactable = _currentIndex < _stageList.Count - 1;
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickStart()
    {
        if (_stageList.Count == 0) return;

        StageData current = _stageList[_currentIndex];

        GameEventBus.Publish(GameEventType.StageSelected, current.Id);
        GameManager.Instance.ChangeState(GameState.GamePlay);
        Time.timeScale = 0f;

        if (!GameManager.Instance.Context.HasSeenTutorial)
        {
            UIManager.Instance.OpenTutorialUI();
        }
        else
        {
            StageStart stageStart = UIManager.Instance.PrepareUI<StageStart>(UIId.Popup.StageStart);
            if (stageStart != null)
            {
                stageStart.Setup(current.Id);
                UIManager.Instance.OpenUI(UIId.Popup.StageStart);
            }
        }
    }

    private void OnClickLeft()
    {
        if (_currentIndex <= 0) return;
        _currentIndex--;
        RefreshAll();
    }

    private void OnClickRight()
    {
        if (_currentIndex >= _stageList.Count - 1) return;
        _currentIndex++;
        RefreshAll();
    }

    // =========================================================================
    // 잠금 여부 판단
    // =========================================================================
    private bool IsStageUnlocked(int index)
    {
        if (index == 0) return true;

        StageData prevStage = _stageList[index - 1];
        var clearStatus = GameManager.Instance.Context.StageClearStatus;

        if (clearStatus.TryGetValue(prevStage.Id, out bool isClear))
            return isClear;

        return false;
    }
}
