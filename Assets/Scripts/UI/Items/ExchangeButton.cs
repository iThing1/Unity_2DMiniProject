using System;
using UnityEngine;
using UnityEngine.UI;
using GameData;

public class ExchangeButton : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private Button _btn;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private ExchangeData _data;
    private Action<ExchangeData> _onExchange;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (_btn != null)
            _btn.onClick.AddListener(OnClickExchange);
    }

    private void OnDestroy()
    {
        if (_btn != null)
            _btn.onClick.RemoveListener(OnClickExchange);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Setup(ExchangeData data, Action<ExchangeData> onExchange)
    {
        _data = data;
        _onExchange = onExchange;
    }

    public void SetInteractable(bool interactable)
    {
        if (_btn != null)
            _btn.interactable = interactable;
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickExchange()
    {
        _onExchange?.Invoke(_data);
    }
}