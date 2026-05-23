using GameData;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public static class SaveLoadController
{
    private const string DEFAULT_SAVE_FILE = "save.json";

    public static void Save<T>(T data)
    {
        Save(DEFAULT_SAVE_FILE, data);
    }

    public static T Load<T>()
    {
        return Load<T>(DEFAULT_SAVE_FILE);
    }

    public static void Save<T>(string fileName, T data)
    {
        string path = GetPath(fileName);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);

        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadController] 저장 완료: {path}");
    }

    public static T Load<T>(string fileName)
    {
        string path = GetPath(fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveLoadController] 세이브 파일이 없습니다: {path}");
            return default;
        }

        string json = File.ReadAllText(path);
        T data = JsonConvert.DeserializeObject<T>(json);

        if (data == null)
        {
            Debug.LogError($"[SaveLoadController] 파일 역직렬화 실패: {path}");
            return default;
        }

        Debug.Log($"[SaveLoadController] 불러오기 완료: {path}");
        return data;
    }

    public static void SaveCurrentGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.Context != null)
        {
            Save(DEFAULT_SAVE_FILE, GameManager.Instance.Context);
        }
    }

    public static bool LoadCurrentGame()
    {
        GameContext context = Load<GameContext>(DEFAULT_SAVE_FILE);
        if (context == null) return false;

        GameManager.Instance.LoadContext(context);
        return true;
    }

    public static bool HasSaveFile()
    {
        return HasSaveFile(DEFAULT_SAVE_FILE);
    }

    public static bool HasSaveFile(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }

    public static void DeleteSave()
    {
        DeleteSave(DEFAULT_SAVE_FILE);
    }

    public static void DeleteSave(string fileName)
    {
        string path = GetPath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveLoadController] 세이브 파일 삭제 완료: {path}");
        }
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}