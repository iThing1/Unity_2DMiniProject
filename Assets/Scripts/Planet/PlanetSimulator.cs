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

// 행성 내부 시뮬레이션을 담당
public class PlanetSimulator : MonoBehaviour
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

    // =========================================================================
    // 런타임 상태 (외부 읽기용)
    // =========================================================================
    public float Prosperity { get; private set; }
    public float Population { get; private set; }
    public float StoredFood { get; private set; }
    public float StoredOre { get; private set; }
    public PlanetState State { get; private set; }
    public bool IsGameOverWarning { get; private set; }
    public bool IsRunning { get; private set; }

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private string _instanceId;
    private string _planetName;
    private float _basePop;

    private float _prosperityChangeRate;
    private float _prosperityIncreaseMax;
    private float _populationChangeRate;
    private float _populationIncreaseMax;

    private float _foodCycleStart;
    private float _foodDeliveredThisCycle;
    private float _oreProducedThisCycle;

    private Coroutine _cycleCoroutine;
    private Coroutine _productionCoroutine;
    private Coroutine _gameOverCoroutine;

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

    // =========================================================================
    // 외부 API: 초기화
    // =========================================================================
    public void Initialize(PlanetData data, string instanceId)
    {
        _instanceId = instanceId;
        _planetName = data.Name;
        _basePop = data.BasePop;

        Population = data.BasePop;
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
        GameEvents.RaisePlanetStateChanged(_instanceId, State);

        IsRunning = true;
        _cycleCoroutine = StartCoroutine(ProsperityCycleRoutine());
        _productionCoroutine = StartCoroutine(RealTimeProductionRoutine());

        Debug.Log($"[PlanetSimulator] '{_planetName}' 초기화 완료. 인구: {Population:F0}, 번영도: {Prosperity:F1}");
    }

    // =========================================================================
    // 번영도 사이클 (10초)
    // =========================================================================
    private IEnumerator ProsperityCycleRoutine()
    {
        while (IsRunning)
        {
            _foodCycleStart = StoredFood;
            float elapsed = 0f;

            while (elapsed < CYCLE_DURATION)
            {
                elapsed += Time.deltaTime;
                GameEvents.RaisePlanetCycleProgress(_instanceId, elapsed / CYCLE_DURATION);
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
        Prosperity = Mathf.Clamp(Prosperity + prosperityDelta, 0f, PROSPERITY_MAX);

        // 인구 갱신
        float populationDelta = (_basePop * satisfaction - _basePop) * _populationChangeRate;
        populationDelta = Mathf.Min(populationDelta, _basePop * _populationIncreaseMax);
        Population = Mathf.Max(Population + populationDelta, 1f);

        StoredFood = Mathf.Max(0f, StoredFood - foodRequired + _foodDeliveredThisCycle);

        _foodDeliveredThisCycle = 0f;
        _oreProducedThisCycle = 0f;

        PlanetState newState = CalcPlanetState(Prosperity);
        if (newState != State)
        {
            State = newState;
            GameEvents.RaisePlanetStateChanged(_instanceId, State);
        }

        if (Prosperity <= 0f)
        {
            TriggerGameOverWarning();
            return;
        }

        if (IsGameOverWarning)
            CancelGameOverWarning();

        Debug.Log($"[PlanetSimulator] '{_planetName}' 사이클. 충족도:{satisfaction:F2}, 번영도:{Prosperity:F1}, 인구:{Population:F0}");
    }

    // =========================================================================
    // 실시간 자원 생산/소비
    // =========================================================================
    private IEnumerator RealTimeProductionRoutine()
    {
        while (IsRunning)
        {
            float dt = Time.deltaTime;

            StoredFood = Mathf.Max(0f, StoredFood - CalcFoodConsumePerSec() * dt);

            float oreGain = CalcOreProductionPerSec() * GetOreMultiplier(State) * dt;
            StoredOre += oreGain;
            _oreProducedThisCycle += oreGain;

            yield return null;
        }
    }

    // =========================================================================
    // 게임오버 판정
    // =========================================================================
    private void TriggerGameOverWarning()
    {
        if (IsGameOverWarning) return;

        IsGameOverWarning = true;
        GameEvents.RaisePlanetGameOverWarning(_instanceId, true);

        if (_gameOverCoroutine != null)
            StopCoroutine(_gameOverCoroutine);

        _gameOverCoroutine = StartCoroutine(GameOverCountdownRoutine());
        Debug.LogWarning($"[PlanetSimulator] '{_planetName}' 멸망 위기! {GAMEOVER_WARNING_TIME}초 유예 시작");
    }

    private IEnumerator GameOverCountdownRoutine()
    {
        yield return new WaitForSeconds(GAMEOVER_WARNING_TIME);

        if (Prosperity <= 0f)
        {
            IsRunning = false;
            State = PlanetState.Destroyed;
            IsGameOverWarning = false;

            GameEvents.RaisePlanetGameOverWarning(_instanceId, false);
            GameEvents.RaisePlanetDestroyed(_instanceId);

            Debug.LogError($"[PlanetSimulator] '{_planetName}' 멸망! 게임오버");
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
        GameEvents.RaisePlanetGameOverWarning(_instanceId, false);

        if (_gameOverCoroutine != null)
        {
            StopCoroutine(_gameOverCoroutine);
            _gameOverCoroutine = null;
        }

        Debug.Log($"[PlanetSimulator] '{_planetName}' 게임오버 경고 해제");
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleFoodDelivered(string instanceId, int amount)
    {
        if (instanceId != _instanceId) return;

        StoredFood += amount;
        _foodDeliveredThisCycle += amount;

        if (IsGameOverWarning && amount > 0)
            CancelGameOverWarning();

        Debug.Log($"[PlanetSimulator] '{_planetName}' 식량 수령 +{amount} / 보유: {StoredFood:F0}");
    }

    private void HandleOreCollected(string instanceId, int amount)
    {
        if (instanceId != _instanceId) return;

        StoredOre = Mathf.Max(0f, StoredOre - amount);
        Debug.Log($"[PlanetSimulator] '{_planetName}' 광석 수거 -{amount} / 잔여: {StoredOre:F0}");
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (next != GameState.GamePlay)
        {
            IsRunning = false;
            StopAllCoroutines();
        }
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
}