using GameData;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("아이템")]
    [SerializeField] private GameObject _achievementItemPrefab;
    [SerializeField] private Transform _content;

    [Header("업적 점수")]
    [SerializeField] private TMP_Text _txtScore;

    [Header("치트 코드")]
    [SerializeField] private TMP_InputField _inputCheatCode;
    [SerializeField] private Button _btnConfirmCode;

    // =========================================================================
    // 상수
    // =========================================================================
    private const string CheatCode = "SpaceExpress";

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly List<AchievementItem> _spawnedItems = new List<AchievementItem>();

    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    protected override void Start()
    {
        base.Start();
        if (_btnConfirmCode != null)
            _btnConfirmCode.onClick.AddListener(OnClickConfirmCode);
    }

    private void OnDestroy()
    {
        if (_btnConfirmCode != null)
            _btnConfirmCode.onClick.RemoveListener(OnClickConfirmCode);
    }

    // =========================================================================
    // 열기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        SpawnItems();

        if (_inputCheatCode != null)
            _inputCheatCode.text = string.Empty;

        if (_txtScore != null)
            _txtScore.text = $"Score : {GameManager.Instance.Context.AchievementScore}";
    }

    protected override void OnBeforeClose()
    {
        ClearItems();
    }

    // =========================================================================
    // 아이템 스폰
    // =========================================================================
    private void SpawnItems()
    {
        ClearItems();

        if (_achievementItemPrefab == null || _content == null) return;

        // 체인 첫 번째 업적만 스폰 (다른 업적의 NextId에 포함되지 않는 것)
        HashSet<string> nextIds = new HashSet<string>();
        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            if (!string.IsNullOrEmpty(data.NextId))
                nextIds.Add(data.NextId);
        }

        foreach (AchievementData data in GameDataManager.Instance.GetAll<AchievementData>())
        {
            // NextId에 포함된 업적은 체인 중간/끝이므로 스킵
            if (nextIds.Contains(data.Id)) continue;

            // 이미 완료된 업적이고 NextId가 없으면 완료된 채로 표시
            // NextId가 있으면 AchievementItem이 자동으로 다음 단계로 넘어감
            string displayId = GetCurrentChainId(data.Id);
            AchievementData displayData = GameDataManager.Instance.Get<AchievementData>(displayId);
            if (displayData == null) continue;

            GameObject instance = Instantiate(_achievementItemPrefab, _content);
            AchievementItem item = instance.GetComponent<AchievementItem>();
            if (item == null) continue;

            item.Setup(displayData);
            _spawnedItems.Add(item);
        }
    }

    // =========================================================================
    // 체인에서 현재 활성화된 업적 ID 반환
    // 완료된 업적은 NextId로 이동, 미완료거나 NextId 없으면 해당 ID 반환
    // =========================================================================
    private string GetCurrentChainId(string startId)
    {
        string currentId = startId;

        while (true)
        {
            if (!GameManager.Instance.Context.IsAchievementCompleted(currentId))
                return currentId;

            AchievementData data = GameDataManager.Instance.Get<AchievementData>(currentId);
            if (data == null || string.IsNullOrEmpty(data.NextId))
                return currentId;

            currentId = data.NextId;
        }
    }

    private void ClearItems()
    {
        foreach (AchievementItem item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }

    // 치트 코드 확인
    private void OnClickConfirmCode()
    {
        if (_inputCheatCode == null) return;

        if (_inputCheatCode.text == CheatCode)
        {
            foreach (AchievementItem item in _spawnedItems)
                item.SetForceCompleteVisible(true);

            GameEventBus.Publish(GameEventType.HiddenCodeFound);
            _inputCheatCode.text = string.Empty;
            Debug.Log("[AchievementUI] 치트 코드 인증 성공");
        }
        else
        {
            Debug.Log("[AchievementUI] 치트 코드 불일치");
        }
    }
}