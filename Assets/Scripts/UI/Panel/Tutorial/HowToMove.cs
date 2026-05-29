using GameData;
using UnityEngine;

public class HowToMove : UIBase
{
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // =========================================================================
    // 닫기
    // =========================================================================
    protected override void OnBeforeClose()
    {
        HowToLoop howToLoop = UIManager.Instance.PrepareUI<HowToLoop>(UIId.Popup.HowToLoop);
        if (howToLoop != null)
            UIManager.Instance.OpenUI(UIId.Popup.HowToLoop);
    }
}