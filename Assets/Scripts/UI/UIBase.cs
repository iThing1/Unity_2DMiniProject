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
    public bool IsOpen { get; private set; }

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected virtual void Awake() { }

    protected virtual void OnEnable()
    {
        RegisterEvents();
    }

    protected virtual void OnDisable()
    {
        UnregisterEvents();
    }

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
    // 가상 메서드 (하위 클래스 오버라이드용)
    // =========================================================================
    public virtual void Setup(object data = null) { }
    protected virtual void RegisterEvents() { }
    protected virtual void UnregisterEvents() { }
    protected virtual void OnBeforeClose() { }

    // =========================================================================
    // 닫기
    // =========================================================================
    public virtual void Open()
    {
        gameObject.SetActive(true);
        IsOpen = true;
    }

    public virtual void Close()
    {
        OnBeforeClose();
        gameObject.SetActive(false);
        IsOpen = false;
    }

    private void OnClickClose()
    {
        Close();
    }
}