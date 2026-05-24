using UnityEngine;
using UnityEngine.UI;
using GameData;

// 개발용 디버그 메뉴 - 빌드 시 비활성화
public class DebugUI : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("디버그 버튼")]
    [SerializeField] private Button _btnForceClear;
    [SerializeField] private Button _btnForceFail;

    [Header("재화 추가")]
    [SerializeField] private Button _btnAddGold;
    [SerializeField] private Button _btnAddIngot;
    [SerializeField] private int _addGoldAmount = 1000;
    [SerializeField] private int _addIngotAmount = 10;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Start()
    {
        if (_btnForceClear != null)
            _btnForceClear.onClick.AddListener(OnClickForceClear);
        if (_btnForceFail != null)
            _btnForceFail.onClick.AddListener(OnClickForceFail);
        if (_btnAddGold != null)
            _btnAddGold.onClick.AddListener(OnClickAddGold);
        if (_btnAddIngot != null)
            _btnAddIngot.onClick.AddListener(OnClickAddIngot);
    }

    private void OnDestroy()
    {
        if (_btnForceClear != null)
            _btnForceClear.onClick.RemoveListener(OnClickForceClear);
        if (_btnForceFail != null)
            _btnForceFail.onClick.RemoveListener(OnClickForceFail);
        if (_btnAddGold != null)
            _btnAddGold.onClick.RemoveListener(OnClickAddGold);
        if (_btnAddIngot != null)
            _btnAddIngot.onClick.RemoveListener(OnClickAddIngot);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickForceClear()
    {
        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogWarning("[DebugUI] LastSelectedStageId가 없습니다.");
            return;
        }

        Debug.Log($"[DebugUI] 강제 스테이지 클리어: {stageId}");
        GameEventBus.Publish(GameEventType.StageClear, stageId);
    }

    private void OnClickForceFail()
    {
        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogWarning("[DebugUI] LastSelectedStageId가 없습니다.");
            return;
        }

        Debug.Log($"[DebugUI] 강제 스테이지 실패: {stageId}");
        GameEventBus.Publish(GameEventType.PlanetDestroyed, $"debug_{stageId}");
    }

    private void OnClickAddGold()
    {
        GameManager.Instance.AddGold(_addGoldAmount);
        Debug.Log($"[DebugUI] 골드 +{_addGoldAmount} (현재: {GameManager.Instance.Context.CurrentGold})");
    }

    private void OnClickAddIngot()
    {
        GameManager.Instance.AddIngot(_addIngotAmount);
        Debug.Log($"[DebugUI] 주괴 +{_addIngotAmount} (현재: {GameManager.Instance.Context.CurrentIngot})");
    }
}