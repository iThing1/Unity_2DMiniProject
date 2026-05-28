using UnityEngine;
using UnityEngine.UI;
using GameData;

// 개발용 디버그 메뉴
public class DebugUI : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
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
        if (_btnAddGold != null)
            _btnAddGold.onClick.AddListener(OnClickAddGold);
        if (_btnAddIngot != null)
            _btnAddIngot.onClick.AddListener(OnClickAddIngot);
    }

    private void OnDestroy()
    {
        if (_btnAddGold != null)
            _btnAddGold.onClick.RemoveListener(OnClickAddGold);
        if (_btnAddIngot != null)
            _btnAddIngot.onClick.RemoveListener(OnClickAddIngot);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================

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