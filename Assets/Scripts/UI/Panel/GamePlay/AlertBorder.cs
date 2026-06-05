using System.Collections;
using UnityEngine;

public class AlertBorder : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private GameObject _alertBorder;
    [SerializeField] private float _alertBlinkInterval = 0.4f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Coroutine _blinkCoroutine;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Start()
    {
        if (_alertBorder != null)
            _alertBorder.SetActive(false);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void StartAlert()
    {
        if (_alertBorder == null) return;
        if (_blinkCoroutine != null) return;

        _alertBorder.SetActive(true);
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopAlert()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        if (_alertBorder != null)
            _alertBorder.SetActive(false);
    }

    // =========================================================================
    // 코루틴
    // =========================================================================
    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            _alertBorder.SetActive(true);
            yield return new WaitForSeconds(_alertBlinkInterval);
            _alertBorder.SetActive(false);
            yield return new WaitForSeconds(_alertBlinkInterval);
        }
    }
}
