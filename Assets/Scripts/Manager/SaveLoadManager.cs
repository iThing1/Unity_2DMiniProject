using System.IO;
using UnityEngine;
using GameData;
using Newtonsoft.Json;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

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
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameEvents.OnStageClear += HandleStageClear;
    }

    private void OnDisable()
    {
        GameEvents.OnStageClear -= HandleStageClear;
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStageClear(string stageId)
    {
        Save();
    }

    // =========================================================================
    // 외부 API
    // =========================================================================

    public void Save()
    {
        GameContext context = GameManager.Instance.Context;
        string path = GetPath();
        string json = JsonConvert.SerializeObject(context, Formatting.Indented);

        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadManager] 저장 완료: {path}");
    }

    public bool Load()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveLoadManager] 세이브 파일이 없습니다.");
            return false;
        }

        string json = File.ReadAllText(GetPath());
        GameContext context = JsonConvert.DeserializeObject<GameContext>(json);

        if (context == null)
        {
            Debug.LogError("[SaveLoadManager] 세이브 파일 역직렬화 실패.");
            return false;
        }

        GameManager.Instance.LoadContext(context);
        Debug.Log("[SaveLoadManager] 불러오기 완료");
        return true;
    }

    public static bool HasSaveFile()
    {
        return File.Exists(GetPath());
    }

    public void DeleteSave()
    {
        if (!HasSaveFile()) return;
        File.Delete(GetPath());
        Debug.Log("[SaveLoadManager] 세이브 파일 삭제 완료");
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, "save.json");
    }
}