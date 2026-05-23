using UnityEngine;
using UnityEngine.UI;
using GameData;

public class StageClear : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("버튼")]
    [SerializeField] private Button _btnGoLobby;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    private void OnEnable()
    {
        GameEvents.OnStageClear += HandleStageClear;
    }

    private void OnDisable()
    {
        GameEvents.OnStageClear -= HandleStageClear;
    }

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
    // 이벤트 핸들러
    // =========================================================================

    // 추가
    private void HandleStageClear(string stageId)
    {
        gameObject.SetActive(true);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickGoLobby()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        GameManager.Instance.ChangeState(GameState.Lobby);
    }
}