// 추가 - 인벤토리 비주얼라이징 테스트용 컴포넌트
// 사용법:
//   1. ShipInventory, InventoryVisualize가 붙어있는 우주선 오브젝트에 이 컴포넌트를 추가
//   2. Inspector에서 _shipInventory 슬롯에 ShipInventory 컴포넌트를 연결
//   3. Play 후 Game 뷰 좌상단 버튼 또는 Inspector 우클릭 ContextMenu로 테스트

#if UNITY_EDITOR
using UnityEngine;
using GameData;

/// <summary>
/// ShipInventory → GameEvents.OnCargoChanged → InventoryVisualize 흐름을 검증하는 테스트 컴포넌트.
/// UNITY_EDITOR 빌드에서만 포함됩니다.
/// </summary>
public class ShipInventoryTester : MonoBehaviour
{
    // =========================================================================
    // Inspector 참조
    // =========================================================================

    [Header("테스트 대상")]
    [SerializeField] private ShipInventory _shipInventory;

    [Header("테스트 설정")]
    [SerializeField] private ShipInventory.CargoType _testCargoType = ShipInventory.CargoType.Food;
    [SerializeField] private int _bulkAmount = 5;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private string _lastLog = "테스트 시작 전";
    private int _eventCallCount = 0;

    // 변경 - GUIStyle 필드로 캐싱 (GUI.skin은 OnGUI 안에서만 유효하므로 첫 OnGUI 호출 시 초기화)
    private GUIStyle _styleTitle;
    private GUIStyle _styleStatus;
    private GUIStyle _styleSection;
    private GUIStyle _styleLog;
    private GUIStyle _styleButton;
    private bool _stylesInitialized = false;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    private void OnEnable()
    {
        GameEvents.OnCargoChanged += HandleCargoChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnCargoChanged -= HandleCargoChanged;
    }

    private void Start()
    {
        if (_shipInventory == null)
        {
            _shipInventory = FindFirstObjectByType<ShipInventory>();

            if (_shipInventory == null)
                Debug.LogError("[ShipInventoryTester] ShipInventory를 찾을 수 없습니다. Inspector에서 직접 연결하세요.");
            else
                Debug.Log("[ShipInventoryTester] ShipInventory 자동 탐색 성공.");
        }

        Log($"테스터 준비 완료. 용량: {_shipInventory?.Capacity}");
    }

    // =========================================================================
    // 이벤트 수신 확인
    // =========================================================================

    private void HandleCargoChanged(int count, int capacity)
    {
        _eventCallCount++;
        Log($"[이벤트 수신 #{_eventCallCount}] 화물: {count} / {capacity}");
    }

    // =========================================================================
    // OnGUI: 런타임 테스트 버튼 (Game 뷰에 표시)
    // =========================================================================

    private void OnGUI()
    {
        // 변경 - GUI.skin 접근이 OnGUI 안에서만 가능하므로 첫 호출 시 스타일 초기화
        if (!_stylesInitialized)
            InitStyles();

        GUILayout.BeginArea(new Rect(10, 10, 400, 520));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>[ShipInventory 테스트]</b>", _styleTitle);
        GUILayout.Space(4);

        if (_shipInventory != null)
            GUILayout.Label($"화물: {_shipInventory.Count} / {_shipInventory.Capacity}  |  이벤트: {_eventCallCount}회", _styleStatus);
        else
            GUILayout.Label("ShipInventory 없음", _styleStatus);

        GUILayout.Space(8);

        GUILayout.Label("── 단일 조작 ──", _styleSection);
        if (GUILayout.Button("식량 1개 추가", _styleButton)) TryAdd(ShipInventory.CargoType.Food);
        if (GUILayout.Button("광석 1개 추가", _styleButton)) TryAdd(ShipInventory.CargoType.Ore);
        if (GUILayout.Button("식량 1개 제거", _styleButton)) TryRemove(ShipInventory.CargoType.Food);
        if (GUILayout.Button("광석 1개 제거", _styleButton)) TryRemove(ShipInventory.CargoType.Ore);

        GUILayout.Space(8);

        GUILayout.Label($"── 코루틴 ({_bulkAmount}개) ──", _styleSection);
        if (GUILayout.Button($"식량 {_bulkAmount}개 코루틴 적재", _styleButton))
            _shipInventory?.StartLoading(ShipInventory.CargoType.Food, _bulkAmount, n => Log($"적재 완료: {n}개"));
        if (GUILayout.Button($"광석 {_bulkAmount}개 코루틴 적재", _styleButton))
            _shipInventory?.StartLoading(ShipInventory.CargoType.Ore, _bulkAmount, n => Log($"적재 완료: {n}개"));
        if (GUILayout.Button("식량 전체 하역", _styleButton))
            _shipInventory?.StartUnloading(ShipInventory.CargoType.Food, onComplete: n => Log($"하역 완료: {n}개"));
        if (GUILayout.Button("전송 중단", _styleButton))
            _shipInventory?.StopTransfer();

        GUILayout.Space(8);

        GUILayout.Label($"<color=yellow>{_lastLog}</color>", _styleLog);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // 변경 - 스타일 초기화를 별도 메서드로 분리 (OnGUI 첫 호출 시 1회만 실행)
    private void InitStyles()
    {
        _styleTitle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18 };
        _styleStatus = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15 };
        _styleSection = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
        _styleLog = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 };
        _styleButton = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 36 };

        _stylesInitialized = true;
    }

    // =========================================================================
    // ContextMenu (Inspector 우클릭)
    // =========================================================================

    [ContextMenu("테스트: 식량 1개 추가")]
    private void CM_AddFood() => TryAdd(ShipInventory.CargoType.Food);

    [ContextMenu("테스트: 광석 1개 추가")]
    private void CM_AddOre() => TryAdd(ShipInventory.CargoType.Ore);

    [ContextMenu("테스트: 식량 전체 하역 (코루틴)")]
    private void CM_UnloadFood() =>
        _shipInventory?.StartUnloading(ShipInventory.CargoType.Food, onComplete: n => Log($"하역 완료: {n}개"));

    [ContextMenu("테스트: 이벤트 수신 횟수 초기화")]
    private void CM_ResetCount() { _eventCallCount = 0; Log("카운트 초기화"); }

    // =========================================================================
    // 내부 유틸
    // =========================================================================

    private void TryAdd(ShipInventory.CargoType type)
    {
        if (_shipInventory == null) { Log("ShipInventory 없음"); return; }

        bool ok = _shipInventory.TryAdd(type);
        Log(ok ? $"{type} 추가 성공 → {_shipInventory.Count}/{_shipInventory.Capacity}"
                : $"추가 실패 (가득 참): {_shipInventory.Count}/{_shipInventory.Capacity}");
    }

    private void TryRemove(ShipInventory.CargoType type)
    {
        if (_shipInventory == null) { Log("ShipInventory 없음"); return; }

        bool ok = _shipInventory.TryRemove(type);
        Log(ok ? $"{type} 제거 성공 → {_shipInventory.Count}/{_shipInventory.Capacity}"
                : $"제거 실패 (해당 화물 없음)");
    }

    private void Log(string msg)
    {
        _lastLog = msg;
        Debug.Log($"[ShipInventoryTester] {msg}");
    }
}
#endif