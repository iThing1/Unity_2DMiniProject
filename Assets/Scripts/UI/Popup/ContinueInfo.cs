using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContinueInfo : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TextMeshProUGUI _txtDate;
    [SerializeField] private TextMeshProUGUI _txtGold;
    [SerializeField] private TextMeshProUGUI _txtIngot;
    [SerializeField] private Button _btnStart;

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

    public override void Setup(object data = null)
    {
        base.Setup(data);

        if (data is GameContext context)
        {
            if (_txtDate != null)
                _txtDate.text = context.SavedAt;

            if (_txtGold != null)
                _txtGold.text = context.CurrentGold.ToString("N0");

            if (_txtIngot != null)
                _txtIngot.text = context.CurrentIngot.ToString("N0");
        }
    }

    private void OnClickStart()
    {
        Close();
        GameEventBus.Publish(GameEventType.ContinueRequested);
        GameManager.Instance.ChangeState(GameState.Lobby);
    }
}
