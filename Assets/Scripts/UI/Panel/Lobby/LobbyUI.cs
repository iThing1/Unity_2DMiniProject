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
    }

    private void OnDestroy()
    {
        if (_btnUpgrade != null)
            _btnUpgrade.onClick.RemoveListener(OnClickUpgrade);
    }

    // =========================================================================
    // StageList 콜백
    // =========================================================================
    private void OnStageChanged(StageData stageData)
    {
        _planetList.Refresh(stageData);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickUpgrade()
    {
        UIManager.Instance.OpenUI(UIId.Panel.Upgrade);
    }
}