using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class StageStartPopup : UIBase
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
    private void OnEnable()
    {
        GameEvents.OnStageSelected += HandleStageSelected;
    }

    private void OnDisable()
    {
        GameEvents.OnStageSelected -= HandleStageSelected;
    }

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

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStageSelected(string stageId)
    {
        StageData data = GameDataManager.Instance.Get<StageData>(stageId);
        if (data == null)
        {
            Debug.LogError($"[StageStartPopup] 스테이지 데이터를 찾지 못했습니다: {stageId}");
            return;
        }

        RefreshUI(data);
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickStart()
    {
        gameObject.SetActive(false);
        GameEvents.RaiseStageStartRequested();
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshUI(StageData data)
    {
        if (_txtName != null)
            _txtName.text = data.Name;

        if (_txtReqGold != null)
            _txtReqGold.text = $"{data.ReqGold:N0}";
    }
}