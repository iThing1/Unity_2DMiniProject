using System.Collections;
using TMPro;
using GameData;
using UnityEngine;

// 업적 달성 알림 팝업
public class AchieveAccomplished : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TMP_Text _txtMessage;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtScore;

    // =========================================================================
    // 상수
    // =========================================================================
    private const float DisplayDuration = 4f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Coroutine _autoCloseCoroutine;

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Show(AchievementData data)
    {
        if (_txtName != null)
            _txtName.text = data.Name;

        if (_txtScore != null)
            _txtScore.text = $"업적 점수 +{data.Score}";

        Open();

        if (_autoCloseCoroutine != null)
            StopCoroutine(_autoCloseCoroutine);

        _autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
    }

    // =========================================================================
    // 자동 닫힘
    // =========================================================================
    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSecondsRealtime(DisplayDuration);
        _autoCloseCoroutine = null;
        UIManager.Instance.CloseUI(UiId);
        AchievementManager.Instance.OnPopupClosed();
    }

    protected override void OnBeforeClose()
    {
        if (_autoCloseCoroutine != null)
        {
            StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = null;
        }
    }
}