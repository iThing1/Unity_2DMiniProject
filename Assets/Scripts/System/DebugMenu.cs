using UnityEngine;
using UnityEngine.UI;

// 개발용 디버깅 툴
public class DebugMenu : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("번영도")]
    [SerializeField] private Button _btnReduceProsperity;

    [Header("재화")]
    [SerializeField] private Button _btnAddGold;
    [SerializeField] private Button _btnAddIngot;
    [SerializeField] private int _addGoldAmount = 1000;
    [SerializeField] private int _addIngotAmount = 10;

    [Header("스테이지")]
    [SerializeField] private Button _btnForceClear;

    // =========================================================================
    // 상수
    // =========================================================================
    private const float ProsperityReduceAmount = 50f;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        if (_btnReduceProsperity != null)
            _btnReduceProsperity.onClick.AddListener(OnClickReduceProsperity);

        if (_btnAddGold != null)
            _btnAddGold.onClick.AddListener(OnClickAddGold);

        if (_btnAddIngot != null)
            _btnAddIngot.onClick.AddListener(OnClickAddIngot);

        if (_btnForceClear != null)
            _btnForceClear.onClick.AddListener(OnClickForceClear);
    }

    private void OnDestroy()
    {
        if (_btnReduceProsperity != null)
            _btnReduceProsperity.onClick.RemoveListener(OnClickReduceProsperity);

        if (_btnAddGold != null)
            _btnAddGold.onClick.RemoveListener(OnClickAddGold);

        if (_btnAddIngot != null)
            _btnAddIngot.onClick.RemoveListener(OnClickAddIngot);

        if (_btnForceClear != null)
            _btnForceClear.onClick.RemoveListener(OnClickForceClear);
    }

    public override void Open()
    {
        base.Open();
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickReduceProsperity()
    {
        PlanetController[] planets = FindObjectsByType<PlanetController>(FindObjectsSortMode.None);
        foreach (PlanetController planet in planets)
            planet.DebugReduceProsperity(ProsperityReduceAmount);

        Debug.Log($"[DebugMenu] 번영도 -{ProsperityReduceAmount}, 행성 수: {planets.Length}");
    }

    private void OnClickAddGold()
    {
        CurrencyManager.Instance.AddGold(_addGoldAmount);
        Debug.Log($"[DebugMenu] 골드 +{_addGoldAmount} (현재: {GameManager.Instance.Context.CurrentGold})");
    }

    private void OnClickAddIngot()
    {
        CurrencyManager.Instance.AddIngot(_addIngotAmount);
        Debug.Log($"[DebugMenu] 주괴 +{_addIngotAmount} (현재: {GameManager.Instance.Context.CurrentIngot})");
    }

    private void OnClickForceClear()
    {
        string stageId = GameManager.Instance.Context.LastSelectedStageId;
        if (string.IsNullOrEmpty(stageId)) return;

        StageManager.Instance.TryClearStage(stageId);
        Debug.Log($"[DebugMenu] 스테이지 강제 클리어: {stageId}");
    }
}