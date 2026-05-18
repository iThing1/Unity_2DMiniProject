using System.Collections;
using UnityEngine;
using GameData;

public enum PlanetState
{
    VeryProsperous,
    Prosperous,
    Neutral,
    Poor,
    Critical,
    Destroyed 
}

public class PlanetController : MonoBehaviour
{
    // =========================================================================
    // 상수
    // =========================================================================
    private const float CYCLE_DURATION = 10f;
    private const float GAMEOVER_WARNING_TIME = 10f;

    private const float PROSPERITY_MAX = 100f;
    private const float PROSPERITY_VERY_HIGH = 80f;
    private const float PROSPERITY_HIGH = 60f;
    private const float PROSPERITY_NEUTRAL = 40f;
    private const float PROSPERITY_LOW = 20f;
    private const float PROSPERITY_CRITICAL = 1f;

    private static readonly float[] ORE_MULTIPLIERS = { 1.5f, 1.3f, 1.0f, 0.8f, 0.6f };
    private const string CONST_PROSPERITY_CHANGE_RATE = "PROSPERITY_CHANGE_RATE";
    private const string CONST_PROSPERITY_INCREASE_MAX = "PROSPERITY_INCREASE_MAX";
    private const string CONST_POPULATION_CHANGE_RATE = "POPULATION_CHANGE_RATE";
    private const string CONST_POPULATION_INCREASE_MAX = "POPULATION_INCREASE_MAX";
    private const string CONST_DOCKING_SPEED = "STATION_DOCKING_SPEED";
    // =========================================================================
    // 런타임 상태 (외부 읽기 전용)
    // =========================================================================
    public string InstanceId { get; private set; }
    public string PlanetName { get; private set; }
    public PlanetSize Size { get; private set; }

    public float Prosperity { get; private set; }
    public float Population { get; private set; }
    public float StoredFood { get; private set; }
    public float StoredOre { get; private set; }
    public PlanetState State { get; private set; }

    public bool IsGameOverWarning { get; private set; }

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private float _basePop;
    private float _prosperityChangeRate;
    private float _prosperityIncreaseMax;
    private float _populationChangeRate;
    private float _populationIncreaseMax;

    private float _foodCycleStart;

    private float _foodConsumedThisCycle;
    private float _oreProducedThisCycle;
    private float _foodDeliveredThisCycle;

    private bool _isRunning = false;
    private Coroutine _cycleCoroutine;
    private Coroutine _gameOverCoroutine;
    private Coroutine _productionCoroutine;

    private ShipController _shipController;
    private ShipInventory _shipInventory;
    private float _dockingSpeedThreshold;
    private bool _isPlayerInside = false;
    private bool _isInteracting = false;
    private Coroutine _interactCoroutine;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnFoodDelivered += HandleFoodDelivered;
        GameEvents.OnOreCollected += HandleOreCollected;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnFoodDelivered -= HandleFoodDelivered;
        GameEvents.OnOreCollected -= HandleOreCollected;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Update()
    {
        if (!_isRunning || !_isPlayerInside || _shipController == null) return;

        bool speedOk = _shipController.Velocity.magnitude <= _dockingSpeedThreshold;

        if (speedOk && !_isInteracting)
            ActivateInteraction();
        else if (!speedOk && _isInteracting)
            DeactivateInteraction();
    }

    // =========================================================================
    // 외부 API: 초기화
    // =========================================================================

    public void Initialize(PlanetData data, string instanceId)
    {
        InstanceId = instanceId;
        PlanetName = data.Name;
        Size = data.Size;

        Population = data.BasePop;
        _basePop = data.BasePop;
        Prosperity = data.Property;
        StoredFood = data.BaseFood;
        StoredOre = data.BaseOre;

        _foodDeliveredThisCycle = 0f;

        var dm = GameDataManager.Instance;
        _prosperityChangeRate = dm.Get<GameConstantData>(CONST_PROSPERITY_CHANGE_RATE)?.Value ?? 0.15f;
        _prosperityIncreaseMax = dm.Get<GameConstantData>(CONST_PROSPERITY_INCREASE_MAX)?.Value ?? 20f;
        _populationChangeRate = dm.Get<GameConstantData>(CONST_POPULATION_CHANGE_RATE)?.Value ?? 0.1f;
        _populationIncreaseMax = dm.Get<GameConstantData>(CONST_POPULATION_INCREASE_MAX)?.Value ?? 0.2f;

        State = CalcPlanetState(Prosperity);
        GameEvents.RaisePlanetStateChanged(InstanceId, State);

        _isRunning = true;
        _cycleCoroutine = StartCoroutine(ProsperityCycleRoutine());
        _productionCoroutine = StartCoroutine(RealTimeProductionRoutine());

        _dockingSpeedThreshold = GameDataManager.Instance.Get<GameConstantData>(CONST_DOCKING_SPEED)?.Value ?? 0.5f;
        CacheShipReferences();


        Debug.Log($"[PlanetController] '{PlanetName}' 초기화 완료. 인구: {Population:F0}, 번영도: {Prosperity:F1}");
    }

    // =========================================================================
    // 핵심 코루틴: 10초 번영도 사이클
    // =========================================================================

    private IEnumerator ProsperityCycleRoutine()
    {
        while (_isRunning)
        {
            _foodCycleStart = StoredFood;
            float elapsed = 0f;

            while (elapsed < CYCLE_DURATION)
            {
                elapsed += Time.deltaTime;
                GameEvents.RaisePlanetCycleProgress(InstanceId, elapsed / CYCLE_DURATION);
                yield return null;
            }

            CalculateCycle();
        }
    }

    private void CalculateCycle()
    {
        float foodRequired = CalcFoodRequired();
        float satisfaction = (_foodCycleStart + _foodDeliveredThisCycle) / Mathf.Max(1f, foodRequired);

        // 번영도 갱신
        float prosperityDelta = (PROSPERITY_MAX * satisfaction - PROSPERITY_MAX) * _prosperityChangeRate;
        prosperityDelta = Mathf.Min(prosperityDelta, _prosperityIncreaseMax);
        float newProsperity = Mathf.Clamp(Prosperity + prosperityDelta, 0f, PROSPERITY_MAX);

        // 인구 갱신
        float populationDelta = (_basePop * satisfaction - _basePop) * _populationChangeRate;
        populationDelta = Mathf.Min(populationDelta, _basePop * _populationIncreaseMax);
        float newPopulation = Mathf.Max(Population + populationDelta, 1f);

        StoredFood = Mathf.Max(0f, StoredFood - foodRequired + _foodDeliveredThisCycle);

        Prosperity = newProsperity;
        Population = newPopulation;

        _foodDeliveredThisCycle = 0f;
        _oreProducedThisCycle = 0f;

        PlanetState newState = CalcPlanetState(Prosperity);
        if (newState != State)
        {
            State = newState;
            GameEvents.RaisePlanetStateChanged(InstanceId, State);
        }

        if (Prosperity <= 0f)
        {
            TriggerGameOverWarning();
            return;
        }

        if (IsGameOverWarning)
            CancelGameOverWarning();

        Debug.Log($"[PlanetController] '{PlanetName}' 사이클. 충족도: {satisfaction:F2}, 번영도: {Prosperity:F1}, 인구: {Population:F0}");
    }

    // =========================================================================
    // 핵심 코루틴: 실시간 자원 생산/소비
    // =========================================================================

    private IEnumerator RealTimeProductionRoutine()
    {
        while (_isRunning)
        {
            float dt = Time.deltaTime;

            // 식량 소비 (초당)
            float foodConsume = CalcFoodConsumePerSec() * dt;
            StoredFood = Mathf.Max(0f, StoredFood - foodConsume);

            // 광석 생산 (초당, 번영도 배율 적용)
            float oreMultiplier = GetOreMultiplier(State);
            float oreGain = CalcOreProductionPerSec() * oreMultiplier * dt;
            StoredOre += oreGain;
            _oreProducedThisCycle += oreGain;

            yield return null;
        }
    }

    // =========================================================================
    // 우주선 상호작용: 트리거 감지 + 저속 체크
    // =========================================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PlanetController] '{PlanetName}' 트리거 진입: {other.gameObject.name} / 태그: {other.tag}");
        if (!other.CompareTag("Player")) return;
        _isPlayerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[PlanetController] '{PlanetName}' 트리거 이탈: {other.gameObject.name} / 태그: {other.tag}");
        if (!other.CompareTag("Player")) return;
        _isPlayerInside = false;
        DeactivateInteraction();
    }

    private void ActivateInteraction()
    {
        _isInteracting = true;

        if (_interactCoroutine != null)
            StopCoroutine(_interactCoroutine);

        _interactCoroutine = StartCoroutine(InteractRoutine());
        Debug.Log($"[PlanetController] '{PlanetName}' 상호작용 시작");
    }

    private void DeactivateInteraction()
    {
        if (!_isInteracting) return;
        _isInteracting = false;

        if (_interactCoroutine != null)
        {
            StopCoroutine(_interactCoroutine);
            _interactCoroutine = null;
        }

        _shipInventory?.StopTransfer();
        Debug.Log($"[PlanetController] '{PlanetName}' 상호작용 중단");
    }

    private IEnumerator InteractRoutine()
    {
        int foodCount = _shipInventory.CountOf(ShipInventory.CargoType.Food);
        if (foodCount > 0)
        {
            _shipInventory.StartUnloading(
                ShipInventory.CargoType.Food,
                onEach: () => GameEvents.RaiseFoodDelivered(InstanceId, 1),
                onComplete: n => Debug.Log($"[PlanetController] '{PlanetName}' 식량 하역 완료: {n}개")
            );
        }

        int oreToLoad = Mathf.Min(
            Mathf.FloorToInt(StoredOre),
            _shipInventory.Capacity - _shipInventory.Count
        );
        if (oreToLoad > 0)
        {
            _shipInventory.StartLoading(
                ShipInventory.CargoType.Ore,
                oreToLoad,
                loaded =>
                {
                    GameEvents.RaiseOreCollected(InstanceId, loaded);
                    Debug.Log($"[PlanetController] '{PlanetName}' 광석 적재 완료: {loaded}개");
                }
            );
        }

        yield return null;
        _isInteracting = false;
        _interactCoroutine = null;
    }

    private void CacheShipReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"[PlanetController] '{PlanetName}' Player 태그 오브젝트를 찾지 못했습니다.");
            return;
        }
        _shipController = player.GetComponent<ShipController>();
        _shipInventory = player.GetComponent<ShipInventory>();
    }

    // =========================================================================
    // 게임오버 판정
    // =========================================================================
    private void TriggerGameOverWarning()
    {
        if (IsGameOverWarning) return;

        IsGameOverWarning = true;
        GameEvents.RaisePlanetGameOverWarning(InstanceId, true);

        if (_gameOverCoroutine != null)
            StopCoroutine(_gameOverCoroutine);

        _gameOverCoroutine = StartCoroutine(GameOverCountdownRoutine());

        Debug.LogWarning($"[PlanetController] '{PlanetName}' 멸망 위기! {GAMEOVER_WARNING_TIME}초 유예 시작");
    }

    private IEnumerator GameOverCountdownRoutine()
    {
        yield return new WaitForSeconds(GAMEOVER_WARNING_TIME);

        if (Prosperity <= 0f)
        {
            _isRunning = false;
            State = PlanetState.Destroyed;
            IsGameOverWarning = false;

            GameEvents.RaisePlanetGameOverWarning(InstanceId, false);
            GameEvents.RaisePlanetDestroyed(InstanceId);

            Debug.LogError($"[PlanetController] '{PlanetName}' 멸망! 게임오버");
        }
        else
        {
            CancelGameOverWarning();
        }
    }

    private void CancelGameOverWarning()
    {
        if (!IsGameOverWarning) return;

        IsGameOverWarning = false;
        GameEvents.RaisePlanetGameOverWarning(InstanceId, false);

        if (_gameOverCoroutine != null)
        {
            StopCoroutine(_gameOverCoroutine);
            _gameOverCoroutine = null;
        }

        Debug.Log($"[PlanetController] '{PlanetName}' 게임오버 경고 해제");
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleFoodDelivered(string instanceId, int amount)
    {
        if (instanceId != InstanceId) return;

        StoredFood += amount;
        _foodDeliveredThisCycle += amount;

        if (IsGameOverWarning && amount > 0)
            CancelGameOverWarning();

        Debug.Log($"[PlanetController] '{PlanetName}' 식량 수령 +{amount} / 보유: {StoredFood:F0}");
    }

    private void HandleOreCollected(string instanceId, int amount)
    {
        if (instanceId != InstanceId) return;

        StoredOre = Mathf.Max(0f, StoredOre - amount);
        Debug.Log($"[PlanetController] '{PlanetName}' 광석 수거 -{amount} / 잔여: {StoredOre:F0}");
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (next != GameState.GamePlay)
        {
            _isRunning = false;
            StopAllCoroutines();
        }

        // GamePlay로 재진입 시 재개 (필요한 경우 외부에서 Initialize 재호출)
    }

    // =========================================================================
    // 계산 유틸
    // =========================================================================

    private float CalcFoodConsumePerSec() => Population / 10000f;
    private float CalcFoodRequired() => CalcFoodConsumePerSec() * CYCLE_DURATION;

    private float CalcOreProductionPerSec() => 1f + (Population / 20000f);

    private PlanetState CalcPlanetState(float prosperity)
    {
        if (prosperity >= PROSPERITY_VERY_HIGH) return PlanetState.VeryProsperous;
        if (prosperity >= PROSPERITY_HIGH) return PlanetState.Prosperous;
        if (prosperity >= PROSPERITY_NEUTRAL) return PlanetState.Neutral;
        if (prosperity >= PROSPERITY_LOW) return PlanetState.Poor;
        if (prosperity >= PROSPERITY_CRITICAL) return PlanetState.Critical;
        return PlanetState.Destroyed;
    }

    private float GetOreMultiplier(PlanetState state)
    {
        switch (state)
        {
            case PlanetState.VeryProsperous: return ORE_MULTIPLIERS[0];
            case PlanetState.Prosperous: return ORE_MULTIPLIERS[1];
            case PlanetState.Neutral: return ORE_MULTIPLIERS[2];
            case PlanetState.Poor: return ORE_MULTIPLIERS[3];
            case PlanetState.Critical: return ORE_MULTIPLIERS[4];
            default: return 0f;
        }
    }

    // =========================================================================
    // 디버그
    // =========================================================================
#if UNITY_EDITOR
    [Header("디버그 전용")]
    [SerializeField] private string _debugPlanetId = "Planet_medium_01";

    private void Start()
    {
        if (!GameDataManager.Instance.IsInitialized)
        {
            GameEvents.OnDataInitialized += DebugInitialize;
            return;
        }
        DebugInitialize();
    }

    private void DebugInitialize()
    {
        GameEvents.OnDataInitialized -= DebugInitialize;
        var data = GameDataManager.Instance.Get<PlanetData>(_debugPlanetId);
        if (data == null)
        {
            Debug.LogWarning($"[PlanetController] 디버그 데이터 없음: {_debugPlanetId}");
            return;
        }
        Initialize(data, gameObject.name);
    }

    [ContextMenu("디버그: 식량 50개 공급")]
    private void Debug_DeliverFood() => HandleFoodDelivered(InstanceId, 50);

    [ContextMenu("디버그: 번영도 0으로 강제")]
    private void Debug_ForceDeath()
    {
        Prosperity = 0f;
        TriggerGameOverWarning();
    }

    [ContextMenu("디버그: 현재 상태 출력")]
    private void Debug_PrintState()
    {
        Debug.Log($"[{PlanetName}] 번영도:{Prosperity:F1} / 인구:{Population:F0} / 식량:{StoredFood:F0} / 광석:{StoredOre:F0} / 상태:{State}");
    }
#endif
}