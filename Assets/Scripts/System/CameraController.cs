using System.Collections;
using UnityEngine;
using GameData;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("카메라 설정")]
    [SerializeField] private float _lerpSpeed = 3f;

    [Header("연출 설정")]
    [SerializeField] private float _focusLerpSpeed = 5f;
    [SerializeField] private float _shakeDuration = 0.5f;
    [SerializeField] private float _focusHoldDuration = 1.5f;
    [SerializeField] private float _focusZOffset = 10f;
    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Camera _camera;
    private float _defaultZ;
    private float _targetZ;
    private float _expansionSpeed;
    private float _elapsedTime;
    private bool _isGamePlay;

    private Vector3 _targetXY = Vector3.zero;
    private bool _isFocusing = false;
    private Coroutine _shakeCoroutine;
    // =========================================================================
    // 외부 API
    // =========================================================================
    public float ViewHalfHeight => Mathf.Abs(_camera.transform.position.z)
        * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

    public float ViewHalfWidth => ViewHalfHeight * _camera.aspect;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<string>(GameEventType.StageSelected, HandleStageSelected);
        GameEventBus.Subscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.StageSelected, HandleStageSelected);
        GameEventBus.Unsubscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
    }

    private void Update()
    {
        TickExpansion();
        ApplyPosition();
    }

    private void InitializeData()
    {
        _defaultZ = -GameConfig.Instance.Settings.CameraHeightDefault;
        _targetZ = _defaultZ;
        transform.position = new Vector3(0f, 0f, _defaultZ);
    }

    public void FocusOn(Vector3 worldPos, System.Action onShakeComplete, System.Action onFocusComplete)
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(FocusRoutine(worldPos, onShakeComplete, onFocusComplete));
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStageSelected(string stageId)
    {
        StageData data = GameDataManager.Instance.Get<StageData>(stageId);
        if (data == null) return;

        _expansionSpeed = data.ExpansionSpeed;
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        _isGamePlay = next == GameState.GamePlay;

        if (_isGamePlay)
        {
            InitializeData();
            _elapsedTime = 0f;
            return;
        }

        _targetZ = _defaultZ;
        _targetXY = Vector3.zero;
        _isFocusing = false;
        transform.position = new Vector3(0f, 0f, _defaultZ);
    }

    // =========================================================================
    // 카메라 확장
    // =========================================================================
    private void TickExpansion()
    {
        if (!_isGamePlay || _isFocusing) return;
        if (_expansionSpeed <= 0f) return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime < 1f) return;

        _elapsedTime -= 1f;
        _targetZ -= _expansionSpeed;
    }

    // =========================================================================
    // 카메라 이동
    // =========================================================================
    private void ApplyPosition()
    {
        float currentZ = transform.position.z;
        float newZ = Mathf.Lerp(currentZ, _targetZ, Time.deltaTime * _lerpSpeed);

        Vector2 currentXY = transform.position;
        Vector2 newXY = Vector2.Lerp(currentXY, _targetXY, Time.deltaTime * _focusLerpSpeed);

        transform.position = new Vector3(newXY.x, newXY.y, newZ);
    }

    private IEnumerator FocusRoutine(Vector3 worldPos, System.Action onShakeComplete, System.Action onFocusComplete)
    {
        _isFocusing = true;
        _targetXY = new Vector3(worldPos.x, worldPos.y, 0f);
        float originalZ = _targetZ;
        float focusZ = _targetZ + _focusZOffset;

        _targetZ = focusZ;
        while (Vector2.Distance(transform.position, _targetXY) > 0.1f)
            yield return null;

        float elapsed = 0f;
        Vector3 originalPos = transform.position;
        float shakeMagnitude = GameConfig.Instance.Settings.CameraShakePower;

        while (elapsed < _shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.position = new Vector3(
                originalPos.x + x,
                originalPos.y + y,
                originalPos.z
            );
            yield return null;
        }

        transform.position = originalPos;
        onShakeComplete?.Invoke();

        yield return new WaitForSecondsRealtime(_focusHoldDuration);
        _shakeCoroutine = null;
        _isFocusing = false;
        _targetXY = Vector3.zero;
        _targetZ = originalZ;

        onFocusComplete?.Invoke();
    }
}