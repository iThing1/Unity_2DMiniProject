using GameData;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private const float TYPING_INTERVAL = 0.05f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private List<IntroData> _introDataList = new List<IntroData>();
    private int _currentIndex = 0;

    private string[] _currentLines;
    private int _currentLineIndex = 0;
    private bool _isTyping = false;
    private bool _isLineComplete = false;

    private Coroutine _typingCoroutine;

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
            OnPressSpace();
    }

    // =========================================================================
    // 열기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        LoadIntroData();
        ShowCurrentScene();
    }

    // =========================================================================
    // 입력 처리
    // =========================================================================
    private void OnPressSpace()
    {
        if (_isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        if (!_isLineComplete) return;

        _currentLineIndex++;

        if (_currentLineIndex >= _currentLines.Length)
        {
            _currentIndex++;
            ShowCurrentScene();
        }
        else
        {
            StartTypingCurrentLine();
        }
    }

    // =========================================================================
    // 데이터 로드
    // =========================================================================
    private void LoadIntroData()
    {
        _introDataList.Clear();
        foreach (IntroData data in GameDataManager.Instance.GetAll<IntroData>())
            _introDataList.Add(data);

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
    private void ShowCurrentScene()
    {
        if (_currentIndex >= _introDataList.Count)
        {
            FinishIntro();
            return;
        }

        IntroData data = _introDataList[_currentIndex];

        _currentLines = data.Message.Split('/');
        _currentLineIndex = 0;

        if (_txtNext != null)
            _txtNext.gameObject.SetActive(false);

        if (_txtMessage != null)
            _txtMessage.text = string.Empty;

        if (_imgBackground != null && !string.IsNullOrEmpty(data.BackgroundPath))
            ResourceManager.Instance.LoadAsset<Sprite>(data.BackgroundPath, OnBackgroundLoaded);

        StartTypingCurrentLine();
    }

    private void OnBackgroundLoaded(Sprite sprite)
    {
        if (_imgBackground != null && sprite != null)
            _imgBackground.sprite = sprite;
    }

    // =========================================================================
    // 타이핑 처리
    // =========================================================================
    private void StartTypingCurrentLine()
    {
        _isLineComplete = false;

        if (_txtNext != null)
            _txtNext.gameObject.SetActive(false);

        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _typingCoroutine = StartCoroutine(TypingRoutine(_currentLines[_currentLineIndex]));
    }

    private IEnumerator TypingRoutine(string line)
    {
        _isTyping = true;

        if (_txtMessage != null)
            _txtMessage.text = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            if (_txtMessage != null)
                _txtMessage.text += line[i];

            yield return new WaitForSeconds(TYPING_INTERVAL);
        }

        _typingCoroutine = null;
        _isTyping = false;
        _isLineComplete = true;

        if (_txtNext != null)
            _txtNext.gameObject.SetActive(true);
    }

    private void CompleteCurrentLine()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        if (_txtMessage != null && _currentLines != null && _currentLineIndex < _currentLines.Length)
            _txtMessage.text = _currentLines[_currentLineIndex];

        _isTyping = false;
        _isLineComplete = true;

        if (_txtNext != null)
            _txtNext.gameObject.SetActive(true);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickSkip()
    {
        FinishIntro();
    }

    // =========================================================================
    // 인트로 종료
    // =========================================================================
    private void FinishIntro()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        GameManager.Instance.ChangeState(GameState.Lobby);
        Destroy(gameObject);
    }
}