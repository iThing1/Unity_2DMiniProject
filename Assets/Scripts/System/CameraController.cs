using UnityEngine;
using GameData;

// 행성 스폰 시마다 Z축으로 멀어지며 시야를 넓힘
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("카메라 설정")]
    [SerializeField] private float _lerpSpeed = 3f;         // Z 이동 lerp 속도

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Camera _camera;
    private float _defaultZ;
    private float _targetZ;
    private float _expansionSpeed;
    private float _elapsedTime;
    private bool _isGamePlay;

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

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void InitializeData()
    {
        _defaultZ = -GameConfig.Instance.Settings.CameraHeightDefault;
        _targetZ = _defaultZ;
        transform.position = new Vector3(0f, 0f, _defaultZ);
    }

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
        transform.position = new Vector3(0f, 0f, _defaultZ);
    }

    // =========================================================================
    // 카메라 확장
    // =========================================================================

    private void TickExpansion()
    {
        if (!_isGamePlay) return;
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
        if (Mathf.Approximately(currentZ, _targetZ)) return;

        float newZ = Mathf.Lerp(currentZ, _targetZ, Time.deltaTime * _lerpSpeed);
        transform.position = new Vector3(0f, 0f, newZ);
    }
}