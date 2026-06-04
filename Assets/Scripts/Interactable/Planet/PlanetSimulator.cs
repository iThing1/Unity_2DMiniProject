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
    private const float PROSPERITY_VERY_HIGH = 80f;
    private const float PROSPERITY_HIGH = 60f;
    private const float PROSPERITY_NEUTRAL = 40f;
    private const float PROSPERITY_LOW = 20f;
    private const float PROSPERITY_CRITICAL = 1f;

    private static readonly float[] ORE_MULTIPLIERS = { 1.3f, 1.1f, 1.0f, 0.7f, 0.5f };

    // =========================================================================
    // 런타임 상태 (외부 읽기용)
    // =========================================================================
    public float Prosperity { get; private set; }
    public float Population { get; private set; }
    public float StoredFood { get; private set; }
    public float StoredOre { get; private set; }
    public float CycleProgress { get; private set; }
    public PlanetState State { get; private set; }
    public bool IsGameOverWarning { get; private set; }
    public bool IsRunning { get; private set; }

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private string _instanceId;
    private string _planetName;
    private float _basePop;

    private float _prosperityMax;
    private float _prosperityChangeRate;
    private float _prosperityIncreaseMax;
    private float _populationChangeRate;
    private float _populationIncreaseMax;
    private float _cycleDuration;
    private float _alertTime;
    private float _foodConsumeBase;
    private float _foodConsumeRate;
    private float _oreProduceRate;

    private float _foodCycleStart;
    private float _foodDeliveredThisCycle;
    private float _oreProducedThisCycle;

    private Coroutine _gameOverCoroutine;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
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
        Prosperity = data.Prosperity;
        StoredFood = data.BaseFood;
        StoredOre = data.BaseOre;

        _foodDeliveredThisCycle = 0f;
        CycleProgress = 0f;

        LoadConstants();
        State = CalcPlanetState(Prosperity);

        IsRunning = true;
        StartCoroutine(ProsperityCycleRoutine());
        StartCoroutine(RealTimeProductionRoutine());
    }

    // =========================================================================
    // 외부 API: 식량 전달 / 광석 수거
    // =========================================================================
    public void DeliverFood(int amount)
    {
        StoredFood += amount;
        _foodDeliveredThisCycle += amount;

        if (IsGameOverWarning && amount > 0)
            CancelWarning();
    }

    public void CollectOre(int amount)
    {
        StoredOre = Mathf.Max(0f, StoredOre - amount);
    }

    // =========================================================================
    // 번영도 사이클
    // =========================================================================
    private IEnumerator ProsperityCycleRoutine()
    {
        while (IsRunning)
        {
            _foodCycleStart = StoredFood;
            _foodDeliveredThisCycle = 0f;
            float elapsed = 0f;

            while (elapsed < _cycleDuration)
            {
                elapsed += Time.deltaTime;
                CycleProgress = elapsed / _cycleDuration;
                yield return null;
            }

            CalculateCycle();
        }
    }

    private void CalculateCycle()
    {
        float foodRequired = CalcFoodRequired();
        float foodSupplied = _foodCycleStart + _foodDeliveredThisCycle;
        float satisfaction = foodSupplied / Mathf.Max(1f, foodRequired);

        // 번영도 갱신
        float prosperityDelta = (_prosperityMax * satisfaction - _prosperityMax) * _prosperityChangeRate;
        prosperityDelta = Mathf.Min(prosperityDelta, _prosperityIncreaseMax);
        Prosperity = Mathf.Clamp(Prosperity + prosperityDelta, 0f, _prosperityMax);

        // 인구 갱신
        float populationDelta = (_basePop * satisfaction - _basePop) * _populationChangeRate;
        populationDelta = Mathf.Min(populationDelta, _basePop * _populationIncreaseMax);
        Population = Mathf.Max(Population + populationDelta, 1f);

        _foodDeliveredThisCycle = 0f;
        _oreProducedThisCycle = 0f;

        State = CalcPlanetState(Prosperity);

        if (Prosperity <= 0f)
        {
            TriggerGameOverWarning();
            return;
        }

        if (IsGameOverWarning)
            CancelWarning();
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
        GameEventBus.Publish(GameEventType.PlanetWarning, _instanceId, true);

        if (_gameOverCoroutine != null)
            StopCoroutine(_gameOverCoroutine);

        _gameOverCoroutine = StartCoroutine(GameOverCountdownRoutine());
    }

    private IEnumerator GameOverCountdownRoutine()
    {
        yield return new WaitForSeconds(_alertTime);

        if (Prosperity <= 0f)
        {
            IsRunning = false;
            State = PlanetState.Destroyed;
            IsGameOverWarning = false;

            GameEventBus.Publish(GameEventType.PlanetWarning, _instanceId, false);
            GameEventBus.Publish(GameEventType.PlanetDestroyed, _instanceId, transform.position);
        }
        else
        {
            CancelWarning();
        }
    }

    private void CancelWarning()
    {
        if (!IsGameOverWarning) return;

        IsGameOverWarning = false;
        GameEventBus.Publish(GameEventType.PlanetWarning, _instanceId, false);

        if (_gameOverCoroutine != null)
        {
            StopCoroutine(_gameOverCoroutine);
            _gameOverCoroutine = null;
        }
    }

    // =========================================================================
    // 계산 유틸
    // =========================================================================
    private float CalcFoodConsumePerSec() => _foodConsumeBase + (Population * _foodConsumeRate);
    private float CalcFoodRequired() => CalcFoodConsumePerSec() * _cycleDuration;
    private float CalcOreProductionPerSec() => Population * _oreProduceRate;

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

    private void LoadConstants()
    {
        var c = GameConfig.Instance.Constants;

        _prosperityMax = c.PlanetProsperityMax;
        _prosperityChangeRate = c.ProsperityChangeRate;
        _prosperityIncreaseMax = c.ProsperityIncreaseMax;
        _populationChangeRate = c.PopulationChangeRate;
        _populationIncreaseMax = c.PopulationIncreaseMax;
        _cycleDuration = c.PlanetConsumeInterval;
        _alertTime = c.PlanetGameoverTime;
        _foodConsumeBase = c.FoodConsumeBase;
        _foodConsumeRate = c.FoodConsumeRate;
        _oreProduceRate = c.OreProdRate;
    }

    // =========================================================================
    // 디버깅 퉅
    // =========================================================================
    public void DebugReduceProsperity(float amount)
    {
        Prosperity = Mathf.Max(0f, Prosperity - amount);
        State = CalcPlanetState(Prosperity);

        if (Prosperity <= 0f)
            TriggerGameOverWarning();
    }
}