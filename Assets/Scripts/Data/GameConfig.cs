using GameData;
using UnityEngine;

public class GameConfig : MonoBehaviour
{
    public static GameConfig Instance { get; private set; }

    // =========================================================================
    // 프로퍼티
    // =========================================================================
    public GameConstants Constants { get; private set; }
    public GameSetting Settings { get; private set; }

    // =========================================================================
    // 스테이지 클리어 점수 배율 상수
    // =========================================================================
    public const int GoldScoreMultiplier = 1;
    public const int IngotScoreMultiplier = 10;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        if (Instance != null)
        { 
            Destroy(gameObject); 
            return;
        }
        Instance = this;
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    public void Initialize()
    {
        CacheConstants();
        CacheSetting();
    }

    // =========================================================================
    // 캐싱 메서드
    // =========================================================================
    private void CacheConstants()
    {
        Constants = new GameConstants
        {
            // 우주선
            FuelConsumeRate = GetConstant("FUEL_CONSUME_RATE", 50f),
            OverheatDuration = GetConstant("OVERHEAT_DURATION", 5f),
            ShipAcceleration = GetConstant("SHIP_ACCELERATION", 3f),
            ShipBoostAccel = GetConstant("SHIP_BOOST_ACCELERATION", 10f),

            // 행성
            ProsperityChangeRate = GetConstant("PROSPERITY_CHANGE_RATE", 0.15f),
            ProsperityIncreaseMax = GetConstant("PROSPERITY_INCREASE_MAX", 20f),
            PopulationChangeRate = GetConstant("POPULATION_CHANGE_RATE", 0.1f),
            PopulationIncreaseMax = GetConstant("POPULATION_INCREASE_MAX", 0.2f),
            PlanetGameoverTime = GetConstant("PLANET_GAMEOVER_TIME", 10f),
            PlanetConsumeInterval = GetConstant("PLANET_CONSUME_INTERVAL", 20f),
            PlanetProsperityMax = GetConstant("PLANET_PROSPERITY_MAX", 100f),

            // 정거장 / 화물
            OreToIngotRatio = GetConstant("ORE_TO_INGOT_RATIO", 10f),
            StationDockingRange = GetConstant("STATION_DOCKING_RANGE", 5f),

            // 행성 생산/소비 계수
            FoodConsumeBase = GetConstant("FOOD_CONSUME_BASE", 0f),
            FoodConsumeRate = GetConstant("FOOD_CONSUME_RATE", 0.00005f),
            OreProdRate = GetConstant("ORE_PRODUCE_RATE", 0.00003f),
        };
    }

    private void CacheSetting()
    {
        Settings = new GameSetting
        {
            CameraHeightDefault = GetSetting("CAM_HEIGHT_DEFAULT", 20f),
            CameraShakePower = GetSetting("CAM_SHAKE_PWR", 0.2f),
            SoundBackgroundVolume = GetSetting("SOUND_BACKGROUND_VOLUME", 100f),
            SoundEffectVolume = GetSetting("SOUND_EFFECT_VOLUME", 100f),
        };
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================

    // TODO: LoggerManager 를 만들어서 로그 관리 -> 유니티에 의존하지 않도록 개선
    private float GetConstant(string id, float fallback)
    {
        GameConstantData data = GameDataManager.Instance.Get<GameConstantData>(id);
        if (data == null)
            Debug.LogWarning($"[GameConfig] 상수 키 없음: '{id}' → 기본값 {fallback} 사용"); 
        return data?.Value ?? fallback;
    }

    private float GetSetting(string id, float fallback)
    {
        GameSettingData data = GameDataManager.Instance.Get<GameSettingData>(id);
        if (data == null)
            Debug.LogWarning($"[GameConfig] 설정 키 없음: '{id}' → 기본값 {fallback} 사용");
        return data?.DefaultValue ?? fallback;
    }
}