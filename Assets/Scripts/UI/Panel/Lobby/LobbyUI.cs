using GameData;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private StageList _stageList;
    [SerializeField] private PlanetList _planetList;

    [Header("업그레이드 버튼")]
    [SerializeField] private Button _btnUpgrade;

    [Header("통계 버튼")]
    [SerializeField] private Button _btnScoreboard;

    [Header("업적 버튼")]
    [SerializeField] private Button _btnAchievement;

    private string _currentStageId;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        _stageList.OnStageChanged = OnStageChanged;
        _stageList.Initialize();

        if (_btnUpgrade != null)
            _btnUpgrade.onClick.AddListener(OnClickUpgrade);

        if (_btnScoreboard != null)
            _btnScoreboard.onClick.AddListener(OnClickScoreboard);

        if (_btnAchievement != null)
            _btnAchievement.onClick.AddListener(OnClickAchievement);
    }

    private void OnDestroy()
    {
        if (_btnUpgrade != null)
            _btnUpgrade.onClick.RemoveListener(OnClickUpgrade);

        if (_btnScoreboard != null)
            _btnScoreboard.onClick.RemoveListener(OnClickScoreboard);

        if (_btnAchievement != null)
            _btnAchievement.onClick.RemoveListener(OnClickAchievement);
    }

    // =========================================================================
    // StageList 콜백
    // =========================================================================
    private void OnStageChanged(StageData stageData)
    {
        _currentStageId = stageData?.Id;
        _planetList.Refresh(stageData);

        if (UIManager.Instance.IsOpen(UIId.Popup.Scoreboard))
        {
            Scoreboard scoreboard = UIManager.Instance.GetUI<Scoreboard>(UIId.Popup.Scoreboard);
            if (scoreboard != null)
                scoreboard.Setup(_currentStageId);
        }
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickUpgrade()
    {
        UIManager.Instance.OpenUI(UIId.Panel.Upgrade);
    }

    private void OnClickScoreboard()
    {
        if (string.IsNullOrEmpty(_currentStageId)) return;

        Scoreboard scoreboard = UIManager.Instance.PrepareUI<Scoreboard>(UIId.Popup.Scoreboard);
        if (scoreboard != null)
        {
            scoreboard.Setup(_currentStageId);
            UIManager.Instance.OpenUI(UIId.Popup.Scoreboard);
        }
    }

    private void OnClickAchievement()
    {
        UIManager.Instance.OpenUI(UIId.Panel.Achievement);
    }
}