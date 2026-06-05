using UnityEngine;
using GameData;

// 행성 멸망 위기 시 타이머 경고 표시
public class PlanetWarningIndicator : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("Warning Image")]
    [SerializeField] private GameObject _warningRoot;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private string _instanceId;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEventBus.Subscribe<string, bool>(GameEventType.PlanetWarning, HandlePlanetWarning);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string, bool>(GameEventType.PlanetWarning, HandlePlanetWarning);
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    public void Initialize(string instanceId)
    {
        _instanceId = instanceId;
        _warningRoot?.SetActive(false);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandlePlanetWarning(string instanceId, bool isWarning)
    {
        if (_instanceId != instanceId) return;
        _warningRoot?.SetActive(isWarning);
    }
}