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
    [SerializeField] private Button _btnUpgrade;

    [Header("행성 리스트")]
    [SerializeField] private Transform _planetListContent;
    [SerializeField] private GameObject _planetItemPrefab;

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
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    protected override void Start()
    {
        base.Start();
        LoadStageList();
        RefreshUI();

        if (_btnStart != null)
            _btnStart.onClick.AddListener(OnClickStart);
        if (_btnLeft != null)
            _btnLeft.onClick.AddListener(OnClickLeft);
        if (_btnRight != null)
            _btnRight.onClick.AddListener(OnClickRight);
        if (_btnUpgrade != null)
            _btnUpgrade.onClick.AddListener(OnClickUpgrade);
    }

    private void OnDestroy()
    {
        if (_btnStart != null)
            _btnStart.onClick.RemoveListener(OnClickStart);
        if (_btnLeft != null)
            _btnLeft.onClick.RemoveListener(OnClickLeft);
        if (_btnRight != null)
            _btnRight.onClick.RemoveListener(OnClickRight);
        if (_btnUpgrade != null)
            _btnUpgrade.onClick.RemoveListener(OnClickUpgrade);
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

        StageStart stageStart = UIManager.Instance.PrepareUI<StageStart>(UIId.Popup.StageStart);
        if (stageStart != null)
        {
            stageStart.Setup(current.Id);
            UIManager.Instance.OpenUI(UIId.Popup.StageStart);
        }

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

    private void OnClickUpgrade()
    {
        UIManager.Instance.OpenUI(UIId.Panel.Upgrade);
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
                Destroy(item.gameObject);
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