using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum SlideDirection { None, Left, Right, Up, Down }

public class UIBase : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private string _uiId;
    [SerializeField] private Button _btnClose;

    [Header("Animation")]
    [SerializeField] private SlideDirection _slideDirection = SlideDirection.None;
    [SerializeField] private float _slideDuration = 0.25f;

    public string UiId => _uiId;
    public bool IsOpen { get; private set; }

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private RectTransform _rectTransform;
    private Coroutine _slideCoroutine;
    private Vector2 _defaultPos;
    private bool _isClosing;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected virtual void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
            _defaultPos = _rectTransform.anchoredPosition;
    }

    protected virtual void OnEnable()
    {
        RegisterEvents();
    }

    protected virtual void OnDisable()
    {
        UnregisterEvents();
    }

    protected virtual void Start()
    {
        if (_btnClose != null)
            _btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnDestroy()
    {
        if (_btnClose != null)
            _btnClose.onClick.RemoveListener(OnClickClose);
    }

    // =========================================================================
    // 가상 메서드 (하위 클래스 오버라이드용)
    // =========================================================================
    public virtual void Setup(object data = null) { }
    protected virtual void RegisterEvents() { }
    protected virtual void UnregisterEvents() { }
    protected virtual void OnBeforeClose() { }

    // =========================================================================
    // 닫기
    // =========================================================================
    public virtual void Open()
    {
        _isClosing = false;
        gameObject.SetActive(true);
        IsOpen = true;

        if (_slideDirection == SlideDirection.None)
            return;

        if (_rectTransform != null)
            _rectTransform.anchoredPosition = GetOffScreenPos();

        StartSlide(_defaultPos);
    }

    public virtual void Close()
    {
        if (_isClosing) return;

        OnBeforeClose();
        if (_slideDirection == SlideDirection.None)
        {
            gameObject.SetActive(false);
            IsOpen = false;
            return;
        }

        _isClosing = true;
        IsOpen = false;
        StartSlide(GetOffScreenPos(), OnSlideOutComplete);
    }

    // =========================================================================
    // 슬라이드 코루틴
    // =========================================================================
    private void StartSlide(Vector2 targetPos, Action onComplete = null)
    {
        if (_slideCoroutine != null)
            StopCoroutine(_slideCoroutine);

        _slideCoroutine = StartCoroutine(SlideRoutine(targetPos, onComplete));
    }

    private IEnumerator SlideRoutine(Vector2 targetPos, Action onComplete)
    {
        Vector2 startPos = _rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < _slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / _slideDuration);
            float eased = EaseOutCubic(t);
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            yield return null;
        }

        _rectTransform.anchoredPosition = targetPos;
        _slideCoroutine = null;
        onComplete?.Invoke();
    }

    private void OnSlideOutComplete()
    {
        gameObject.SetActive(false);
        IsOpen = false;
        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _defaultPos;
    }

    // =========================================================================
    // 화면 밖 위치 계산
    // =========================================================================
    private Vector2 GetOffScreenPos()
    {
        if (_rectTransform == null) return Vector2.zero;

        float w = _rectTransform.rect.width;
        float h = _rectTransform.rect.height;

        switch (_slideDirection)
        {
            case SlideDirection.Right: return _defaultPos + new Vector2(w + Screen.width, 0f);
            case SlideDirection.Left: return _defaultPos - new Vector2(w + Screen.width, 0f);
            case SlideDirection.Up: return _defaultPos + new Vector2(0f, h + Screen.height);
            case SlideDirection.Down: return _defaultPos - new Vector2(0f, h + Screen.height);
            default: return _defaultPos;
        }
    }

    // =========================================================================
    // 이징 함수
    // =========================================================================
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void OnClickClose()
    {
        Close();
    }
}