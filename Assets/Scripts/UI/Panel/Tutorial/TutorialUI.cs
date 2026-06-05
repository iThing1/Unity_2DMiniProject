using UnityEngine;
using UnityEngine.UI;
using GameData;

public class TutorialUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private Button _btnStart;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        UIManager.Instance.OpenAllUIByBindState("GamePlay");
    }

    protected override void Start()
    {
        base.Start();
        if (_btnStart != null)
            _btnStart.onClick.AddListener(OnClickStart);
    }

    private void OnDestroy()
    {
        if (_btnStart != null)
            _btnStart.onClick.RemoveListener(OnClickStart);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickStart()
    {
        UIManager.Instance.CloseAllPopupsByBindState("GamePlay");

        GameManager.Instance.Context.HasSeenTutorial = true;
        GameManager.Instance.SaveCurrentGame();

        Close();

        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        StageStart stageStart = UIManager.Instance.PrepareUI<StageStart>(UIId.Popup.StageStart);
        if (stageStart != null)
        {
            stageStart.Setup(stageId);
            UIManager.Instance.OpenUI(UIId.Popup.StageStart);
        }
    }
}