using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class StageClear : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("버튼")]
    [SerializeField] private Button _btnGoLobby;

    [Header("클리어 기록")]
    [SerializeField] private TMP_Text _txtClearTime;
    [SerializeField] private TMP_Text _txtScore;

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
    // 데이터 주입
    // =========================================================================
    public override void Setup(object data = null)
    {
        base.Setup(data);

        if (data is StageClearRecord record)
        {
            if (_txtClearTime != null)
            {
                int minutes = Mathf.FloorToInt(record.ClearTime / 60f);
                int seconds = Mathf.FloorToInt(record.ClearTime % 60f);
                _txtClearTime.text = $"Clear Time : {minutes:00}:{seconds:00}";
            }

            if (_txtScore != null)
                _txtScore.text = $"Score: {record.Score:N0}";
        }
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