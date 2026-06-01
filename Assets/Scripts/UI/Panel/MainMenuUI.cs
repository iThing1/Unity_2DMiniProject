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

        bool hasAnySave = false;
        for (int i = 0; i < SaveLoadController.MAX_SLOTS; i++)
        {
            if (SaveLoadController.HasSlot(i))
            {
                hasAnySave = true;
                break;
            }
        }
        if (_btnContinue != null)
            _btnContinue.interactable = hasAnySave;
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
        SaveSlot slotSelect = UIManager.Instance.PrepareUI<SaveSlot>(UIId.Popup.ContinueInfo);
        if (slotSelect != null)
        {
            slotSelect.Setup(SlotSelectMode.NewGame);
            UIManager.Instance.OpenUI(UIId.Popup.ContinueInfo);
        }
    }

    private void OnClickContinue()
    {
        SaveSlot slotSelect = UIManager.Instance.PrepareUI<SaveSlot>(UIId.Popup.ContinueInfo);
        if (slotSelect != null)
        {
            slotSelect.Setup(SlotSelectMode.Continue);
            UIManager.Instance.OpenUI(UIId.Popup.ContinueInfo);
        }
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