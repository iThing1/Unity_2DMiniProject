using UnityEngine;
using UnityEngine.UI;
using GameData;

public class SettingUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("버튼")]
    [SerializeField] private Button _btnToggle;
    [SerializeField] private Button _btnBGMOnOff;
    [SerializeField] private Button _btnHome;

    [Header("토글 패널")]
    [SerializeField] private GameObject _panel;

    [Header("BGM 상태 아이콘")]
    [SerializeField] private GameObject _iconBGMOn;
    [SerializeField] private GameObject _iconBGMOff;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private bool _isPanelOpen = false;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        if (_btnToggle != null)
            _btnToggle.onClick.AddListener(OnClickToggle);
        if (_btnBGMOnOff != null)
            _btnBGMOnOff.onClick.AddListener(OnClickBGMOnOff);
        if (_btnHome != null)
            _btnHome.onClick.AddListener(OnClickHome);

        SetPanelOpen(false);
    }

    private void OnDestroy()
    {
        if (_btnToggle != null)
            _btnToggle.onClick.RemoveListener(OnClickToggle);
        if (_btnBGMOnOff != null)
            _btnBGMOnOff.onClick.RemoveListener(OnClickBGMOnOff);
        if (_btnHome != null)
            _btnHome.onClick.RemoveListener(OnClickHome);
    }

    // =========================================================================
    // 열기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        SetPanelOpen(false);
        RefreshBGMIcon();
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickToggle()
    {
        SetPanelOpen(!_isPanelOpen);
    }

    private void OnClickBGMOnOff()
    {
        SoundManager.Instance.ToggleBGM(GameManager.Instance.CurrentState);
        RefreshBGMIcon();
    }

    private void OnClickHome()
    {
        SetPanelOpen(false);
        GameState currentState = GameManager.Instance.CurrentState;
        if (currentState == GameState.GamePlay)
            GameManager.Instance.ReturnToLobby();
        else if (currentState == GameState.Lobby)
            GameManager.Instance.ReturnToMainMenu();
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void SetPanelOpen(bool open)
    {
        _isPanelOpen = open;
        if (_panel != null)
            _panel.SetActive(open);
    }

    private void RefreshBGMIcon()
    {
        bool isBGMOn = SoundManager.Instance.IsBGMOn;
        if (_iconBGMOn != null) _iconBGMOn.SetActive(isBGMOn);
        if (_iconBGMOff != null) _iconBGMOff.SetActive(!isBGMOn);
    }
}