using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class StageStart : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("UI 연결")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtReqGold;
    [SerializeField] private Button _btnStart;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();
        if (_btnStart != null)
            _btnStart.onClick.AddListener(OnClickStart);
    }

    private void OnDestroy()
    {
        if (_btnStart != null)
            _btnStart.onClick.RemoveListener(OnClickStart);
    }

    protected override void RegisterEvents()
    {
        GameEventBus.Subscribe<string>(GameEventType.StageSelected, HandleStageSelected);
    }

    protected override void UnregisterEvents()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.StageSelected, HandleStageSelected);
    }

    // =========================================================================
    // 데이터 주입
    // =========================================================================
    public override void Setup(object data = null)
    {
        if (data is string stageId)
            RefreshUI(stageId);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStageSelected(string stageId)
    {
        Setup(stageId);
        Time.timeScale = 0f;
        Open();
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickStart()
    {
        Time.timeScale = 1f;
        Close();
        GameEventBus.Publish(GameEventType.StageStartRequested);
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshUI(string stageId)
    {
        StageData data = GameDataManager.Instance.Get<StageData>(stageId);
        if (data == null)
        {
            Debug.LogError($"[StageStart] 스테이지 데이터를 찾지 못했습니다: {stageId}");
            return;
        }

        if (_txtName != null)
            _txtName.text = data.Name;

        if (_txtReqGold != null)
            _txtReqGold.text = $"Goal: {data.ReqGold:N0}";
    }
}