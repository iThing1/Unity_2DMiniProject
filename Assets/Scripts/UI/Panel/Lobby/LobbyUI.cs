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
    }

    private void OnDestroy()
    {
        if (_btnUpgrade != null)
            _btnUpgrade.onClick.RemoveListener(OnClickUpgrade);

        if (_btnScoreboard != null)
            _btnScoreboard.onClick.RemoveListener(OnClickScoreboard);
    }

    // =========================================================================
    // StageList 콜백
    // =========================================================================
    private void OnStageChanged(StageData stageData)
    {
        _currentStageId = stageData?.Id;
        _planetList.Refresh(stageData);
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
}