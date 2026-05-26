using GameData;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

public static class SaveLoadController
{
    public const int MAX_SLOTS = 4;
    private const string SLOT_FILE_FORMAT = "save_slot_{0}.json";

    // =========================================================================
    // 슬롯 기반 API
    // =========================================================================
    public static void SaveSlot(int slotIndex, GameContext context)
    {
        if (!IsValidSlot(slotIndex)) return;
        context.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        SaveToFile(GetSlotFileName(slotIndex), context);
    }

    public static GameContext LoadSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return null;
        return LoadFromFile<GameContext>(GetSlotFileName(slotIndex));
    }

    public static void DeleteSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return;
        DeleteFile(GetSlotFileName(slotIndex));
    }

    public static bool HasSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return false;
        return FileExists(GetSlotFileName(slotIndex));
    }

    public static GameContext PeekSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return null;
        return LoadFromFile<GameContext>(GetSlotFileName(slotIndex));
    }

    // =========================================================================
    // 파일 I/O (내부 유틸)
    // =========================================================================
    private static void SaveToFile<T>(string fileName, T data)
    {
        string path = GetPath(fileName);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadController] 저장 완료: {path}");
    }

    private static T LoadFromFile<T>(string fileName)
    {
        string path = GetPath(fileName);
        if (!File.Exists(path)) return default;

        string json = File.ReadAllText(path);
        T data = JsonConvert.DeserializeObject<T>(json);

        if (data == null)
        {
            Debug.LogError($"[SaveLoadController] 파일 역직렬화 실패: {path}");
            return default;
        }

        return data;
    }

    private static void DeleteFile(string fileName)
    {
        string path = GetPath(fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    private static bool FileExists(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }

    // =========================================================================
    // 유틸
    // =========================================================================
    private static string GetSlotFileName(int slotIndex)
    {
        return string.Format(SLOT_FILE_FORMAT, slotIndex);
    }

    private static bool IsValidSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < MAX_SLOTS) return true;
        Debug.LogWarning($"[SaveLoadController] 유효하지 않은 슬롯 인덱스: {slotIndex}");
        return false;
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}