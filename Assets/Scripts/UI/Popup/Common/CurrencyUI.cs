using TMPro;
using UnityEngine;

// 재화(골드/주괴) 표시 공통 컴포넌트
public class CurrencyUI : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("재화 텍스트")]
    [SerializeField] private TMP_Text _txtGold;
    [SerializeField] private TMP_Text _txtIngot;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEventBus.Subscribe<int>(GameEventType.GoldChanged, HandleGoldChanged);
        GameEventBus.Subscribe<int>(GameEventType.IngotChanged, HandleIngotChanged);

        // 활성화 시 현재 값으로 즉시 갱신
        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInitialized)
        {
            var ctx = GameManager.Instance.Context;
            RefreshGold(ctx.CurrentGold);
            RefreshIngot(ctx.CurrentIngot);
        }
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<int>(GameEventType.GoldChanged, HandleGoldChanged);
        GameEventBus.Unsubscribe<int>(GameEventType.IngotChanged, HandleIngotChanged);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleGoldChanged(int gold)
    {
        RefreshGold(gold);
    }

    private void HandleIngotChanged(int ingot)
    {
        RefreshIngot(ingot);
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshGold(int gold)
    {
        if (_txtGold == null) return;
        _txtGold.text = gold.ToString("N0");
    }

    private void RefreshIngot(int ingot)
    {
        if (_txtIngot == null) return;
        _txtIngot.text = ingot.ToString("N0");
    }
}