using UnityEngine;
using UnityEngine.UI;
using GameData;

public class MainMenuUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("버튼")]
    [SerializeField] private Button _btnNewGame;
    [SerializeField] private Button _btnContinue;
    [SerializeField] private Button _btnQuit;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        if (_btnNewGame != null)
            _btnNewGame.onClick.AddListener(OnClickNewGame);
        if (_btnContinue != null)
            _btnContinue.onClick.AddListener(OnClickContinue);
        if (_btnQuit != null)
            _btnQuit.onClick.AddListener(OnClickQuit);

        if (_btnContinue != null)
            _btnContinue.interactable = SaveLoadController.HasSaveFile();
    }

    private void OnDestroy()
    {
        if (_btnNewGame != null)
            _btnNewGame.onClick.RemoveListener(OnClickNewGame);
        if (_btnContinue != null)
            _btnContinue.onClick.RemoveListener(OnClickContinue);
        if (_btnQuit != null)
            _btnQuit.onClick.RemoveListener(OnClickQuit);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickNewGame()
    {
        SaveLoadController.DeleteSave();
        GameEventBus.Publish(GameEventType.NewGameRequested);
        GameManager.Instance.ChangeState(GameState.Lobby);
    }

    private void OnClickContinue()
    {
        GameEventBus.Publish(GameEventType.ContinueRequested);
        GameManager.Instance.ChangeState(GameState.Lobby);
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}