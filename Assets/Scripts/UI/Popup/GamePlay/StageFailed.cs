using UnityEngine;
using UnityEngine.UI;
using GameData;
using TMPro;

public class StageFailed : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("버튼")]
    [SerializeField] private Button _btnGoLobby;

    [Header("Value Info")]
    [SerializeField] private TMP_Text _txtValue;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    protected override void Start()
    {
        base.Start();

        if (_btnGoLobby != null)
            _btnGoLobby.onClick.AddListener(OnClickGoLobby);
    }

    private void OnDestroy()
    {
        if (_btnGoLobby != null)
            _btnGoLobby.onClick.RemoveListener(OnClickGoLobby);
    }


    // =========================================================================
    // 열기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        Refresh();
    }

    private void Refresh()
    {
        float playTime = Time.realtimeSinceStartup - StageManager.Instance.StageStartTime;

        if (_txtValue != null)
            _txtValue.text = MathUtility.FormatTime(playTime);
    }


    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickGoLobby()
    {
        Time.timeScale = 1f;
        Close();
        GameManager.Instance.ChangeState(GameState.Lobby);
    }
}