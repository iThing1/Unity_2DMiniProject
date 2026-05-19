using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class LobbyUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("스테이지 정보")]
    [SerializeField] private TMP_Text _txtStageName;
    [SerializeField] private Button _btnStart;
    [SerializeField] private Button _btnLeft;
    [SerializeField] private Button _btnRight;

    [Header("행성 리스트")]
    [SerializeField] private Transform _planetListContent;
    [SerializeField] private GameObject _planetItemPrefab;

    [Header("재화")]
    [SerializeField] private TMP_Text _txtGold;
    [SerializeField] private TMP_Text _txtIngot;

    [Header("잠금 표시")]
    [SerializeField] private float _lockedAlpha = 0.3f; // TODO: 스프라이트 교체 방식으로 변경 고려

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private List<StageData> _stageList = new List<StageData>();
    private List<PlanetItem> _planetItems = new List<PlanetItem>();
    private int _currentIndex = 0;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnGoldChanged += HandleGoldChanged;

        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInitialized)
           HandleDataInitialized();
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnGoldChanged -= HandleGoldChanged;
    }

    protected override void Start()
    {
        base.Start();
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
    // 이벤트 핸들러
    // =========================================================================
    private void HandleDataInitialized()
    { 
        var ctx = GameManager.Instance.Context;

        RefreshGold(ctx.CurrentGold);
        RefreshIngot(ctx.CurrentIngot);

        LoadStageList();
        RefreshUI();
    }

    private void HandleGoldChanged(float gold)
    {
        RefreshStartButton();
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickStart()
    {
        if (_stageList.Count == 0) return;

        StageData current = _stageList[_currentIndex];

        GameManager.Instance.ChangeState(GameState.GamePlay);
        UIManager.Instance.OpenUI<StageStart>(UIId.Popup.StageStart);
        GameEvents.RaiseStageSelected(current.Id);
    }

    private void OnClickLeft()
    {
        if (_currentIndex <= 0) return;
        _currentIndex--;
        RefreshUI();
    }

    private void OnClickRight()
    {
        if (_currentIndex >= _stageList.Count - 1) return;
        _currentIndex++;
        RefreshUI();
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


    private void RefreshUI()
    {
        if (_stageList.Count == 0) return;

        StageData current = _stageList[_currentIndex];

        RefreshStageName(current);
        RefreshStartButton();
        RefreshNavButtons();
        RefreshPlanetList(current);
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

        StageData current = _stageList[_currentIndex];
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

    private void RefreshPlanetList(StageData data)
    {
        foreach (PlanetItem item in _planetItems)
        {
            if (item != null)
                DestroyImmediate(item.gameObject);
        }
        _planetItems.Clear();

        if (_planetItemPrefab == null || _planetListContent == null) return;

        string[] planetIds = data.GetPlanetList();

        foreach (string planetId in planetIds)
        {
            PlanetData planetData = GameDataManager.Instance.Get<PlanetData>(planetId);
            if (planetData == null) continue;

            GameObject instance = Instantiate(_planetItemPrefab, _planetListContent);

            PlanetItem item = instance.GetComponentInChildren<PlanetItem>();

            if (item == null) continue;

            item.Setup(planetData);
            _planetItems.Add(item);
        }
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