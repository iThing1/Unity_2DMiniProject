using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Confirm : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TMP_Text _txtMessage;
    [SerializeField] private Button _btnOkay;
    [SerializeField] private Button _btnCancel;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Action _onConfirm;
    private Action _onCancel;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        if (_btnOkay != null)
            _btnOkay.onClick.AddListener(OnClickOkay);
        if (_btnCancel != null)
            _btnCancel.onClick.AddListener(OnClickCancel);
    }

    private void OnDestroy()
    {
        if (_btnOkay != null)
            _btnOkay.onClick.RemoveListener(OnClickOkay);
        if (_btnCancel != null)
            _btnCancel.onClick.RemoveListener(OnClickCancel);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public override void Setup(object data = null)
    {
        base.Setup(data);

        if (data is ConfirmData confirmData)
        {
            _onConfirm = confirmData.OnConfirm;
            _onCancel = confirmData.OnCancel;

            if (_txtMessage != null)
                _txtMessage.text = confirmData.Message;
        }
    }

    protected override void OnBeforeClose()
    {
        _onConfirm = null;
        _onCancel = null;
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickOkay()
    {
        Action confirm = _onConfirm;
        Close();
        confirm?.Invoke();
    }

    private void OnClickCancel()
    {
        Action cancel = _onCancel;
        Close();
        cancel?.Invoke();
    }
}

public class ConfirmData
{
    public string Message;
    public Action OnConfirm;
    public Action OnCancel;

    public ConfirmData(string message, Action onConfirm, Action onCancel = null)
    {
        Message = message;
        OnConfirm = onConfirm;
        OnCancel = onCancel;
    }
}