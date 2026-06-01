using GameData;
using UnityEngine;

// 특정 상황에 맞게 UI를 여닫는 헬퍼 클래스
public static class UIManagerExtension
{
    public static void OnGameStateChanged(this UIManager uiManager, GameState prev, GameState next)
    {
        bool showCurrencyUI = (next == GameState.Lobby || next == GameState.GamePlay);
        uiManager.ShowCurrencyUI(showCurrencyUI);

        bool showSettingUI = (next == GameState.Lobby || next == GameState.GamePlay);
        uiManager.ShowSettingUI(showSettingUI);
    }

    // =========================================================================
    // 인트로
    // =========================================================================
    public static void OpenIntroUI(this UIManager uiManager)
    {
        uiManager.OpenUI(UIId.VeryFront.Intro);
        SoundManager.Instance.PlayBGM("Sounds/BGM/Intro");
    }

    // =========================================================================
    // 재화 UI
    // =========================================================================
    public static void ShowCurrencyUI(this UIManager uiManager, bool active)
    {
        string currencyUiId = UIId.Panel.Currency;

        if (active)
        {
            uiManager.OpenUI(currencyUiId);

            GameObject currencyUI = uiManager.GetUI<UIBase>(currencyUiId)?.gameObject;
            if (currencyUI != null)
                currencyUI.transform.SetAsLastSibling();
        }
        else
        {
            uiManager.CloseUI(currencyUiId);
        }
    }
    // =========================================================================
    // Setting UI
    // =========================================================================
    public static void ShowSettingUI(this UIManager uiManager, bool active)
    {
        string settingUiId = UIId.Panel.Setting;

        if (active)
        {
            uiManager.OpenUI(settingUiId);

            GameObject settingUI = uiManager.GetUI<UIBase>(settingUiId)?.gameObject;
            if (settingUI != null)
                settingUI.transform.SetAsLastSibling();
        }
        else
        {
            uiManager.CloseUI(settingUiId);
        }
    }

    // =========================================================================
    // 튜토리얼
    // =========================================================================
    public static void OpenTutorialUI(this UIManager uiManager)
    {
        HowToMove howToMove = uiManager.PrepareUI<HowToMove>(UIId.Popup.HowToMove);
        if (howToMove != null)
            uiManager.OpenUI(UIId.Popup.HowToMove);
    }

    // =========================================================================
    // BindState 기반 일괄 처리
    // =========================================================================
    public static void OpenAllUIByBindState(this UIManager uiManager, string bindState)
    {
        if (bindState == null) return;

        foreach (UIData data in GameDataManager.Instance.GetAll<UIData>())
        {
            if (data.BindState != bindState) continue;
            uiManager.OpenUI(data.Id);
        }
    }

    public static void CloseAllPopupsByBindState(this UIManager uiManager, string bindState)
    {
        if (bindState == null) return;

        foreach (UIData data in GameDataManager.Instance.GetAll<UIData>())
        {
            if (data.BindState != bindState) continue;
            if (data.Type != UIType.Popup) continue;
            uiManager.CloseUI(data.Id);
        }
    }

    public static void SetUIByBindState(this UIManager uiManager, string bindState, bool activate)
    {
        if (bindState == null) return;

        foreach (UIData data in GameDataManager.Instance.GetAll<UIData>())
        {
            if (data.BindState != bindState) continue;

            if (activate)
            {
                if (data.Auto) uiManager.OpenUI(data.Id);
            }
            else
            {
                uiManager.CloseUI(data.Id);
            }
        }
    }
}