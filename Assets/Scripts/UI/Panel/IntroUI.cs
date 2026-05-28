using GameData;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인트로 연출 UI
// NewGame 시 1회만 재생 후 Destroy
public class IntroUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("배경")]
    [SerializeField] private Image _imgBackground;

    [Header("메시지")]
    [SerializeField] private TMP_Text _txtMessage;
    [SerializeField] private TMP_Text _txtNext;

    [Header("스킵")]
    [SerializeField] private Button _btnSkip;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private List<IntroData> _introDataList = new List<IntroData>();
    private int _currentIndex = 0;
    private bool _isMessageDone = false;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Awake()
    {
        base.Awake();
        if (_btnSkip != null)
            _btnSkip.onClick.AddListener(OnClickSkip);

        if (_txtNext != null)
            _txtNext.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_btnSkip != null)
            _btnSkip.onClick.RemoveListener(OnClickSkip);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            OnPressNext();
    }

    // =========================================================================
    // 열기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        LoadIntroData();
        ShowCurrent();
    }

    // =========================================================================
    // 데이터 로드
    // =========================================================================
    private void LoadIntroData()
    {
        _introDataList.Clear();
        foreach (IntroData data in GameDataManager.Instance.GetAll<IntroData>())
            _introDataList.Add(data);

        // Id 기준 정렬
        _introDataList.Sort(CompareIntroDataById);
        _currentIndex = 0;
    }

    private int CompareIntroDataById(IntroData a, IntroData b)
    {
        return string.Compare(a.Id, b.Id, System.StringComparison.Ordinal);
    }

    // =========================================================================
    // 현재 장면 표시
    // =========================================================================
    private void ShowCurrent()
    {
        if (_currentIndex >= _introDataList.Count)
        {
            FinishIntro();
            return;
        }

        IntroData data = _introDataList[_currentIndex];

        if (_txtMessage != null)
            _txtMessage.text = data.Message.Replace("/", "\n");

        if (_txtNext != null)
            _txtNext.gameObject.SetActive(false);

        _isMessageDone = false;

        if (_imgBackground != null && !string.IsNullOrEmpty(data.BackgroundPath))
            ResourceManager.Instance.LoadAsset<Sprite>(data.BackgroundPath, OnBackgroundLoaded);

        SetMessageDone();
    }

    private void OnBackgroundLoaded(Sprite sprite)
    {
        if (_imgBackground != null && sprite != null)
            _imgBackground.sprite = sprite;
    }

    private void SetMessageDone()
    {
        _isMessageDone = true;

        if (_txtNext != null)
            _txtNext.gameObject.SetActive(true);
    }

    // =========================================================================
    // 입력 처리
    // =========================================================================
    private void OnPressNext()
    {
        if (!_isMessageDone) return;

        _currentIndex++;
        ShowCurrent();
    }

    private void OnClickSkip()
    {
        FinishIntro();
    }

    // =========================================================================
    // 인트로 종료
    // =========================================================================
    private void FinishIntro()
    {
        GameManager.Instance.ChangeState(GameState.Lobby);
        Destroy(gameObject);
    }
}