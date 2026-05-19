using GameData;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("Canvas Root")]
    [SerializeField] private Transform _mainRoot;
    [SerializeField] private Transform _popupRoot;
    [SerializeField] private Transform _veryFrontRoot;

    [Header("UI 프리팹")]
    [SerializeField] private GameObject[] _uiPrefabs;
    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly Dictionary<string, GameObject> _prefabMap = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _createdUIDic = new Dictionary<string, GameObject>();
    private readonly HashSet<string> _openedUISet = new HashSet<string>();

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

        RegisterPrefab();
    }

    private void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }
   
    private void RegisterPrefab()
    {
        foreach (GameObject prefab in _uiPrefabs)
        {
            if (prefab == null) continue;

            UIBase uiBase = prefab.GetComponent<UIBase>();
            if (uiBase == null)
            {
                Debug.LogWarning($"[UIManager] UIBase 컴포넌트가 없습니다: {prefab.name}");
                continue;
            }

            _prefabMap[uiBase.UiId] = prefab;
        }
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleDataInitialized()
    {
        SpawnAutoUI(GameManager.Instance.CurrentState);
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        ProcessUIForState(prev, false);
        SpawnAutoUI(next);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void OpenUI<T>(string uiId) where T : MonoBehaviour
    {
        if (_createdUIDic.TryGetValue(uiId, out GameObject existing))
        {
            existing.SetActive(true);
            _openedUISet.Add(uiId);
            return;
        }

        SpawnUI(uiId);
    }

    public void CloseUI(string uiId)
    {
        if (!_createdUIDic.TryGetValue(uiId, out GameObject panel)) return;

        panel.SetActive(false);
        _openedUISet.Remove(uiId);
    }

    public T GetUI<T>(string uiId) where T : MonoBehaviour
    {
        if (!_createdUIDic.TryGetValue(uiId, out GameObject panel)) return null;
        return panel.GetComponent<T>();
    }

    public bool IsOpen(string uiId)
    {
        return _openedUISet.Contains(uiId);
    }

    // =========================================================================
    // 스폰 관련
    // =========================================================================
    private void SpawnAutoUI(GameState state)
    {
        ProcessUIForState(state, true);
    }

    private void SpawnUI(string uiId)
    {
        if (!_prefabMap.TryGetValue(uiId, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"[UIManager] 등록되지 않은 UI: {uiId}");
            return;
        }

        UIData data = GameDataManager.Instance.Get<UIData>(uiId);
        if (data == null)
        {
            Debug.LogError($"[UIManager] UIData를 찾지 못했습니다: {uiId}");
            return;
        }

        Transform root = GetRootTransform(data.Type);
        GameObject instance = Instantiate(prefab, root);

        _createdUIDic[uiId] = instance;
        _openedUISet.Add(uiId);
    }

    // =========================================================================
    // 상태별 UI 처리 공통 메서드
    // =========================================================================
    private void ProcessUIForState(GameState state, bool activate)
    {
        string bindState = StateToString(state);
        if (bindState == null) return;

        foreach (UIData data in GameDataManager.Instance.GetAll<UIData>())
        {
            if (data.BindState != bindState) continue;

            if (activate)
            {
                if (!data.AutoSpawn) continue;

                if (_createdUIDic.TryGetValue(data.Id, out GameObject existing))
                {
                    existing.SetActive(true);
                    _openedUISet.Add(data.Id);
                    continue;
                }

                SpawnUI(data.Id);
            }
            else
            {
                if (!_createdUIDic.TryGetValue(data.Id, out GameObject panel)) continue;

                panel.SetActive(false);
                _openedUISet.Remove(data.Id);
            }
        }
    }

    // =========================================================================
    // 유틸
    // =========================================================================
    private Transform GetRootTransform(UIType uiType)
    {
        switch (uiType)
        {
            case UIType.Main: return _mainRoot;
            case UIType.Popup: return _popupRoot;
            case UIType.VeryFront: return _veryFrontRoot;
            default: return _mainRoot;
        }
    }

    private string StateToString(GameState state)
    {
        switch (state)
        {
            case GameState.Loading: return "Loading";
            case GameState.MainMenu: return "MainMenu";
            case GameState.Lobby: return "Lobby";
            case GameState.GamePlay: return "GamePlay";
            default: return null;
        }
    }
}