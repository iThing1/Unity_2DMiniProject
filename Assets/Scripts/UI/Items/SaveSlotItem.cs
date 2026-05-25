using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotItem : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TextMeshProUGUI _txtDate;
    [SerializeField] private TextMeshProUGUI _txtGold;
    [SerializeField] private TextMeshProUGUI _txtIngot;
    [SerializeField] private TextMeshProUGUI _txtLastStage;
    protected override void Start()
    {
        base.Start();
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
}
