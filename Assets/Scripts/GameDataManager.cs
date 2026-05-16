using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    private readonly Dictionary<Type, object> _tables = new Dictionary<Type, object>();

    public bool IsInitialized { get; private set; } = false;
    public event Action OnInitialized;

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
            // 런타임에 제네릭 메서드를 호출하기 위해 리플렉션 사용
            var method = typeof(GameDataManager)
                .GetMethod(nameof(RegisterTable))
                .MakeGenericMethod(type);

            tasks.Add((Task)method.Invoke(this, new object[] { address }));
        }

        await Task.WhenAll(tasks);

        IsInitialized = true;
        Debug.Log("<color=cyan><b>[GameDataManager]</b> 데이터 로드 완료!</color>");
        OnInitialized?.Invoke();
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

    // =========================================================================
    // 내부 로드 구현
    // =========================================================================
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
public interface IGameData
{
    string Id { get; }
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string wrapped = $"{{\"Items\":{json}}}";
        return JsonUtility.FromJson<Wrapper<T>>(wrapped).Items;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}