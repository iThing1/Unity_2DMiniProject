using UnityEngine;
using UnityEngine.UI;

public class UIBase : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private string _uiId;
    [SerializeField] private Button _btnClose;

    public string UiId => _uiId;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected virtual void Start()
    {
        if (_btnClose != null)
            _btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnDestroy()
    {
        if (_btnClose != null)
            _btnClose.onClick.RemoveListener(OnClickClose);
    }

    // =========================================================================
    // 닫기
    // =========================================================================
    private void OnClickClose()
    {
        OnBeforeClose();
        gameObject.SetActive(false);
    }

    protected virtual void OnBeforeClose() { }
}