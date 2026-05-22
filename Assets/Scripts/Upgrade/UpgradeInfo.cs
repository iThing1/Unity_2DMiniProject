using UnityEngine;
using TMPro;
using GameData;

// 업그레이드 툴팁 UI
public class UpgradeInfo : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TMP_Text _txtLevel;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtGold;
    [SerializeField] private TMP_Text _txtIngot;

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Show(string upgradeId, int slotLevel)
    {
        if (string.IsNullOrEmpty(upgradeId)) return;

        UpgradeData data = GameDataManager.Instance.Get<UpgradeData>(upgradeId);
        if (data == null) return;

        int currentLevel = GameManager.Instance.GetUpgradeLevel(upgradeId);
        bool isAlreadyDone = currentLevel >= slotLevel;

        if (_txtLevel != null)
            _txtLevel.text = $"Lv.{currentLevel}";

        if (_txtName != null)
            _txtName.text = data.Name;

        if (isAlreadyDone)
        {
            if (_txtGold != null) _txtGold.text = "DONE";
            if (_txtIngot != null) _txtIngot.text = "";
        }
        else
        {
            int costLevel = slotLevel - 1;
            float goldCost = data.BaseGoldCost + data.CostIncrease * costLevel;
            float IngotCost = data.BaseIngotCost + data.IngotIncrease * costLevel;

            if (_txtGold != null) _txtGold.text = goldCost.ToAbbreviatedString(0);
            if (_txtIngot != null) _txtIngot.text = IngotCost.ToAbbreviatedString(0);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}