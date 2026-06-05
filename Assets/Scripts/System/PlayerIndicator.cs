using UnityEngine;
using TMPro;
using GameData;

// 플레이어가 화면 밖으로 나갔을 때 화면 가장자리에 방향 화살표와 거리를 표시
public class PlayerIndicator : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("UI 요소")]
    [SerializeField] private RectTransform _arrowRect;
    [SerializeField] private TMP_Text _txtDistance;

    private float _edgePadding = 60f;
    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Transform _playerTransform;
    private Camera _mainCamera;
    private CameraController _cameraController;
    private bool _isGamePlay;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _mainCamera = Camera.main;
        _cameraController = _mainCamera?.GetComponent<CameraController>();
        SetIndicatorVisible(false);
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<Transform>(GameEventType.ShipSpawned, HandleShipSpawned);
        GameEventBus.Subscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<Transform>(GameEventType.ShipSpawned, HandleShipSpawned);
        GameEventBus.Unsubscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
    }

    private void Update()
    {
        if (!_isGamePlay || _playerTransform == null) return;

        UpdateIndicator();
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleShipSpawned(Transform shipTransform)
    {
        _playerTransform = shipTransform;
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        _isGamePlay = next == GameState.GamePlay;

        if (!_isGamePlay)
        {
            _playerTransform = null;
            SetIndicatorVisible(false);
        }
    }

    // =========================================================================
    // 인디케이터 갱신
    // =========================================================================
    private void UpdateIndicator()
    {
        if (_cameraController == null) return;

        float halfW = _cameraController.ViewHalfWidth;
        float halfH = _cameraController.ViewHalfHeight;

        Vector3 playerPos = _playerTransform.position;

        bool isOnScreen = Mathf.Abs(playerPos.x) <= halfW
                       && Mathf.Abs(playerPos.y) <= halfH;

        if (isOnScreen)
        {
            SetIndicatorVisible(false);
            return;
        }

        SetIndicatorVisible(true);

        Vector2 direction = new Vector2(playerPos.x, playerPos.y).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 30f;

        if (_arrowRect != null)
            _arrowRect.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector2 edgePos = GetEdgePosition(direction, halfW, halfH);

        if (_arrowRect != null)
        {
            RectTransform canvasRect = transform.parent as RectTransform;
            if (canvasRect != null)
            {
                float halfUiW = canvasRect.rect.width * 0.5f - _edgePadding;
                float halfUiH = canvasRect.rect.height * 0.5f - _edgePadding;
                float uiX = (edgePos.x / halfW) * halfUiW;
                float uiY = (edgePos.y / halfH) * halfUiH;
                _arrowRect.anchoredPosition = new Vector2(uiX, uiY);
            }
        }

        if (_txtDistance != null)
        {
            float distance = Vector2.Distance(Vector2.zero, new Vector2(playerPos.x, playerPos.y));
            _txtDistance.text = $"{Mathf.FloorToInt(distance)}m";
        }
    }

    // =========================================================================
    // 화면 가장자리 위치 계산
    // =========================================================================
    private Vector2 GetEdgePosition(Vector2 direction, float halfW, float halfH)
    {
        float scaleX = halfW / Mathf.Abs(direction.x + 0.0001f);
        float scaleY = halfH / Mathf.Abs(direction.y + 0.0001f);
        float scale = Mathf.Min(scaleX, scaleY);

        return direction * scale;
    }

    // =========================================================================
    // UI 요소 표시/숨김
    // =========================================================================
    private void SetIndicatorVisible(bool visible)
    {
        if (_arrowRect != null)
            _arrowRect.gameObject.SetActive(visible);

        if (_txtDistance != null)
            _txtDistance.gameObject.SetActive(visible);
    }
}
