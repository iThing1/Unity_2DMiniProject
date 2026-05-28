using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

// 업적 목록 한 줄 아이템
// 달성 시 NextId로 다음 단계 업적으로 자동 갱신
public class AchievementItem : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("이미지")]
    [SerializeField] private Image _imgAchievement;
    [SerializeField] private Image _imgCompleted;

    [Header("텍스트")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtDesc;

    [Header("진행도")]
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _txtProgress;

    [Header("치트")]
    [SerializeField] private Button _btnForceComplete;
    // =========================================================================
    // 내부 상태
    // =========================================================================
    private AchievementData _currentData;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (_btnForceComplete != null)
        {
            _btnForceComplete.gameObject.SetActive(false);
            _btnForceComplete.onClick.AddListener(OnClickForceComplete);
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<string>(GameEventType.AchievementCompleted, HandleAchievementCompleted);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.AchievementCompleted, HandleAchievementCompleted);
    }

    private void OnDestroy()
    {
        if (_btnForceComplete != null)
            _btnForceComplete.onClick.RemoveListener(OnClickForceComplete);
    }
    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Setup(AchievementData data)
    {
        _currentData = data;

        bool isCompleted = GameManager.Instance.Context.IsAchievementCompleted(data.Id);
        int currentValue = GameManager.Instance.Context.GetAchievementProgress(data.Id);

        Refresh(isCompleted, currentValue);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleAchievementCompleted(string achievementId)
    {
        if (_currentData == null) return;
        if (_currentData.Id != achievementId) return;

        // 다음 업적이 있으면 해당 데이터로 갱신
        if (!string.IsNullOrEmpty(_currentData.NextId))
        {
            AchievementData nextData = GameDataManager.Instance.Get<AchievementData>(_currentData.NextId);
            if (nextData != null)
            {
                Setup(nextData);
                return;
            }
        }

        // 다음 업적 없으면 완료 상태로 표시
        Refresh(true, _currentData.TargetValue ?? 0);
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void Refresh(bool isCompleted, int currentValue)
    {
        if (_currentData == null) return;

        if (_txtName != null)
            _txtName.text = _currentData.Name;

        if (_txtDesc != null)
            _txtDesc.text = _currentData.Description;

        if (_imgAchievement != null && !string.IsNullOrEmpty(_currentData.SpritePath))
            ResourceManager.Instance.LoadSpriteFromSheet("AchievementInfo", _currentData.SpritePath, OnSpriteLoaded);

        if (_imgCompleted != null)
            _imgCompleted.gameObject.SetActive(isCompleted);

        switch (_currentData.AchievementType)
        {
            case AchievementType.Stacked:
                if (_progressBar != null)
                {
                    _progressBar.gameObject.SetActive(true);
                    _progressBar.value = isCompleted ? 1f : Mathf.Clamp01((float)currentValue / _currentData.TargetValue ?? 1);
                }

                if (_txtProgress != null)
                {
                    _txtProgress.gameObject.SetActive(true);
                    _txtProgress.text = isCompleted
                        ? $"{_currentData.TargetValue} / {_currentData.TargetValue}"
                        : $"{currentValue} / {_currentData.TargetValue}";
                }
                break;

            case AchievementType.Acomplished:
                if (_progressBar != null)
                    _progressBar.gameObject.SetActive(false);

                if (_txtProgress != null)
                    _txtProgress.gameObject.SetActive(false);
                break;
        }
    }

    private void OnSpriteLoaded(Sprite sprite)
    {
        if (_imgAchievement != null && sprite != null)
            _imgAchievement.sprite = sprite;
    }

    // =========================================================================
    // [Debgu] 클리어 버튼 표시/숨김
    // =========================================================================
    public void SetForceCompleteVisible(bool visible)
    {
        if (_btnForceComplete != null)
            _btnForceComplete.gameObject.SetActive(visible);
    }

    private void OnClickForceComplete()
    {
        if (_currentData == null) return;

        bool completed = GameManager.Instance.Context.CompleteAchievement(_currentData.Id);
        if (completed)
            GameEventBus.Publish(GameEventType.AchievementCompleted, _currentData.Id);
    }
}