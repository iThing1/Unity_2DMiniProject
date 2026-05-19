using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using GameData;
using Newtonsoft.Json;

// =========================================================================
// 추가: 게임 상수 전용 구조체
// =========================================================================
public struct GameConstants
{
    // 우주선
    public float FuelConsumeRate;
    public float FuelRegenRate;
    public float OverheatDuration;
    public float ShipAcceleration;
    public float ShipBoostAccel;
    public float DockingSpeed;

    // 행성
    public float ProsperityChangeRate;
    public float ProsperityIncreaseMax;
    public float PopulationChangeRate;
    public float PopulationIncreaseMax;
    public float PlanetGameoverTime;
    public float PlanetConsumeInterval;
    public float PlanetProsperityMax;

    // 정거장 / 화물
    public float CargoTransferInterval;
    public float OreToIngotRatio;
    public float StationDockingRange;
}

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    private readonly Dictionary<Type, object> _tables = new Dictionary<Type, object>();

    public bool IsInitialized { get; private set; } = false;
    public GameConstants Constants { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task RegisterTable<T>(string address) where T : IGameData
    {
        var table = new Dictionary<string, T>();
        await LoadTableAsync<T>(address, table);
        _tables[typeof(T)] = table;
    }

    public async Task RegisterAllTables(params (string address, Type type)[] entries)
    {
        var tasks = new List<Task>();

        foreach (var (address, type) in entries)
        {
            var method = typeof(GameDataManager)
                .GetMethod(nameof(RegisterTable))
                .MakeGenericMethod(type);

            tasks.Add((Task)method.Invoke(this, new object[] { address }));
        }

        await Task.WhenAll(tasks);

        CacheConstants();

        IsInitialized = true;
        GameEvents.RaiseDataInitialized();
    }

    public T Get<T>(string id) where T : IGameData
    {
        if (_tables.TryGetValue(typeof(T), out var tableObj) &&
            tableObj is Dictionary<string, T> table &&
            table.TryGetValue(id, out var data))
        {
            return data;
        }

        Debug.LogWarning($"[GameDataManager] 데이터를 찾지 못했습니다: [{typeof(T).Name}] id={id}");
        return default;
    }

    public IEnumerable<T> GetAll<T>() where T : IGameData
    {
        if (_tables.TryGetValue(typeof(T), out var tableObj) &&
            tableObj is Dictionary<string, T> table)
        {
            return table.Values;
        }

        Debug.LogWarning($"[GameDataManager] 등록되지 않은 테이블: {typeof(T).Name}");
        return Array.Empty<T>();
    }

    public bool HasTable<T>() where T : IGameData => _tables.ContainsKey(typeof(T));

    private void CacheConstants()
    {
        float Get(string id, float fallback)
        {
            var data = Get<GameConstantData>(id);
            if (data == null)
                Debug.LogWarning($"[GameDataManager] 상수 키 없음: '{id}' → 기본값 {fallback} 사용");
            return data?.Value ?? fallback;
        }

        Constants = new GameConstants
        {
            // 우주선
            FuelConsumeRate = Get("FUEL_CONSUME_RATE", 50f),
            FuelRegenRate = Get("FUEL_REGEN_RATE", 20f),
            OverheatDuration = Get("OVERHEAT_DURATION", 5f),
            ShipAcceleration = Get("SHIP_ACCELERATION", 5f),
            ShipBoostAccel = Get("SHIP_BOOST_ACCELERATION", 20f),
            DockingSpeed = Get("STATION_DOCKING_SPEED", 0.5f),

            // 행성
            ProsperityChangeRate = Get("PROSPERITY_CHANGE_RATE", 0.15f),
            ProsperityIncreaseMax = Get("PROSPERITY_INCREASE_MAX", 20f),
            PopulationChangeRate = Get("POPULATION_CHANGE_RATE", 0.1f),
            PopulationIncreaseMax = Get("POPULATION_INCREASE_MAX", 0.2f),
            PlanetGameoverTime = Get("PLANET_GAMEOVER_TIME", 10f),
            PlanetConsumeInterval = Get("PLANET_CONSUME_INTERVAL", 10f),
            PlanetProsperityMax = Get("PLANET_PROSPERITY_MAX", 100f),

            // 정거장 / 화물
            CargoTransferInterval = Get("CARGO_TRANSFER_INTERVAL", 0.2f),
            OreToIngotRatio = Get("ORE_TO_INGOT_RATIO", 10f),
            StationDockingRange = Get("STATION_DOCKING_RANGE", 5f),
        };
    }

    private async Task LoadTableAsync<T>(string address, Dictionary<string, T> dictionary) where T : IGameData
    {
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            T[] dataArray = JsonHelper.FromJson<T>(handle.Result.text);

            dictionary.Clear();
            foreach (var data in dataArray)
            {
                if (!dictionary.ContainsKey(data.Id))
                    dictionary.Add(data.Id, data);
                else
                    Debug.LogWarning($"[GameDataManager] 중복 Id 무시: [{typeof(T).Name}] id={data.Id}");
            }

            // JSON 텍스트 에셋은 파싱 후 즉시 해제
            Addressables.Release(handle);
        }
        else
        {
            Debug.LogError($"[GameDataManager] 로드 실패: {address}");
        }
    }
}

// =========================================================================
// 공통 인터페이스 & 유틸리티
// =========================================================================
public static class JsonHelper
{
    private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
    };

    public static T[] FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T[]>(json, _settings);
    }
}