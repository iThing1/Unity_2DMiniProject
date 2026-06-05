using GameData;
using UnityEngine;

public class HowToLoop : UIBase
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
        TutorialUI overlay = UIManager.Instance.PrepareUI<TutorialUI>(UIId.VeryFront.TutorialOverlay);
        if (overlay != null)
            UIManager.Instance.OpenUI(UIId.VeryFront.TutorialOverlay);
    }
}