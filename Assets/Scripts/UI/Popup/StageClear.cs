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
    [SerializeField] private Button _btnDetail;

    [Header("클리어 정보")]
    [SerializeField] private TMP_Text _txtPlayTime;
    [SerializeField] private TMP_Text _txtTotalScore;

    [Header("점수 상세")]
    [SerializeField] private GameObject _detail;
    [SerializeField] private TMP_Text _txtEarnGold;
    [SerializeField] private TMP_Text _txtEarnIngot;

    private const int GoldScoreMultiplier = 1;
    private const int IngotScoreMultiplier = 10;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        if (_btnGoLobby != null)
            _btnGoLobby.onClick.AddListener(OnClickGoLobby);

        if (_btnDetail != null)
            _btnDetail.onClick.AddListener(OnClickDetail);

        if (_detail != null)
            _detail.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_btnGoLobby != null)
            _btnGoLobby.onClick.RemoveListener(OnClickGoLobby);

        if (_btnDetail != null)
            _btnDetail.onClick.RemoveListener(OnClickDetail);
    }

    // =========================================================================
    // 데이터 주입
    // =========================================================================
    public override void Setup(object data = null)
    {
        base.Setup(data);

        if (data is StageClearRecord record)
            Refresh(record);
    }

    private void Refresh(StageClearRecord record)
    {
        if (_txtPlayTime != null)
            _txtPlayTime.text = MathUtility.FormatTime(record.ClearTime);

        if (_txtTotalScore != null)
            _txtTotalScore.text = $"{record.Score:N0}";

        if (_txtEarnGold != null)
            _txtEarnGold.text = $"{record.GoldEarned:N0} x {GoldScoreMultiplier} = {record.GoldEarned * GoldScoreMultiplier:N0}";

        if (_txtEarnIngot != null)
            _txtEarnIngot.text = $"{record.IngotEarned:N0} x {IngotScoreMultiplier} = {record.IngotEarned * IngotScoreMultiplier:N0}";
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

    private void OnClickDetail()
    {
        if (_detail != null)
            _detail.SetActive(!_detail.activeSelf);
    }

    // =========================================================================
    // 닫기 전 정리
    // =========================================================================
    protected override void OnBeforeClose()
    {
        if (_detail != null)
            _detail.SetActive(false);
    }
}