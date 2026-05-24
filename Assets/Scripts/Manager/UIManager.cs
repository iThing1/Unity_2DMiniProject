using GameData;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

    [Header("Loading UI")]
    [SerializeField] private GameObject _loadingUIPrefab;

    [Header("HUD")]
    [SerializeField] private GameObject _currencyUIPrefab;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly Dictionary<string, GameObject> _prefabMap = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _createdUIDic = new Dictionary<string, GameObject>();
    private readonly HashSet<string> _openedUISet = new HashSet<string>();
    private GameObject _currencyUIInstance;

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

        SpawnLoadingUI();
        SpawnCurrencyUI();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
        GameEventBus.Subscribe<bool, StationController>(GameEventType.StationInteractionChanged, HandleStationInteractionChanged);
        GameEventBus.Subscribe<string>(GameEventType.StageFailed, HandleStageFailed);
        GameEventBus.Subscribe<string>(GameEventType.StageClear, HandleStageClear);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
        GameEventBus.Unsubscribe<bool, StationController>(GameEventType.StationInteractionChanged, HandleStationInteractionChanged);
        GameEventBus.Unsubscribe<string>(GameEventType.StageFailed, HandleStageFailed);
        GameEventBus.Unsubscribe<string>(GameEventType.StageClear, HandleStageClear);
    }

    // =========================================================================
    // GameManager에서 호출 - UI 프리팹 일괄 로드
    // =========================================================================
    public async Task LoadUIPrefabsAsync()
    {
        var allUIData = GameDataManager.Instance.GetAll<UIData>();
        var tasks = new List<Task>();

        foreach (UIData data in allUIData)
        {
            // Loading UI는 Inspector에서 처리
            if (data.Id == UIId.VeryFront.Loading) continue;

            tasks.Add(LoadAndRegisterPrefab(data));
        }

        await Task.WhenAll(tasks);
        Debug.Log($"[UIManager] UI 프리팹 로드 완료: {_prefabMap.Count}개");
    }

    private async Task LoadAndRegisterPrefab(UIData data)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(data.Name);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            _prefabMap[data.Id] = handle.Result;
        else
            Debug.LogWarning($"[UIManager] UI 프리팹 로드 실패: {data.Name}");
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        ProcessUIForState(prev, false);
        SpawnAutoUI(next);
        RefreshCurrencyUI(next);
    }

    private void HandleStationInteractionChanged(bool isActive, StationController station)
    {
        if (isActive)
        {
            if (station == null)
            {
                Debug.LogWarning("[UIManager] station이 null입니다.");
                return;
            }

            var zoneType = station.CurrentZone;
            if (zoneType != StationZoneType.Left && zoneType != StationZoneType.Right) return;

            bool exists = _createdUIDic.ContainsKey(UIId.Popup.StationUpgrade);

            StationUpgrade upgradeUI = GetUI<StationUpgrade>(UIId.Popup.StationUpgrade);
            if (upgradeUI == null)
            {
                SpawnUI(UIId.Popup.StationUpgrade, hidden: false);
                upgradeUI = GetUI<StationUpgrade>(UIId.Popup.StationUpgrade);

                if (upgradeUI == null) return;
            }

            upgradeUI.Open();
            upgradeUI.Setup(new StationUpgradeData(zoneType.Value, station));
        }
        else
        {
            CloseUI(UIId.Popup.StationUpgrade);
        }
    }

    private void HandleStageFailed(string stageId)
    {
        Time.timeScale = 0f;
        OpenUI<StageFailed>(UIId.Popup.StageFailed);
    }

    private void HandleStageClear(string stageId)
    {
        Time.timeScale = 0f;
        OpenUI<StageClear>(UIId.Popup.StageClear);
    }
    // =========================================================================
    // 외부 API
    // =========================================================================
    public void OpenUI<T>(string uiId) where T : MonoBehaviour
    {
        if (_createdUIDic.TryGetValue(uiId, out GameObject existing))
        {
            UIBase ui = existing.GetComponent<UIBase>();

            if (ui != null) ui.Open();
            else existing.SetActive(true);

            _openedUISet.Add(uiId);
            return;
        }

        SpawnUI(uiId);
        if (_createdUIDic.TryGetValue(uiId, out GameObject newInstance))
        {
            UIBase newUi = newInstance.GetComponent<UIBase>();
            if (newUi != null) newUi.Open();
        }
    }

    public void CloseUI(string uiId)
    {
        if (!_createdUIDic.TryGetValue(uiId, out GameObject panel)) return;
        if (panel == null)
        {
            _createdUIDic.Remove(uiId);
            _openedUISet.Remove(uiId);
            return;
        }

        UIBase ui = panel.GetComponent<UIBase>();
        if (ui != null) ui.Close();
        else panel.SetActive(false);

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

    private void SpawnUI(string uiId, bool hidden = false)
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

        instance.SetActive(true);
        if (hidden)
            instance.SetActive(false);

        _createdUIDic[uiId] = instance;
        if (!hidden)
            _openedUISet.Add(uiId);
    }

    private void SpawnCurrencyUI()
    {
        if (_currencyUIPrefab == null)
        {
            Debug.LogWarning("[UIManager] CurrencyUIPrefab이 연결되지 않았습니다.");
            return;
        }

        _currencyUIInstance = Instantiate(_currencyUIPrefab, _mainRoot);
        _currencyUIInstance.SetActive(false);
    }

    private void RefreshCurrencyUI(GameState state)
    {
        if (_currencyUIInstance == null) return;

        bool isVisible = state == GameState.Lobby || state == GameState.GamePlay;   
        _currencyUIInstance.SetActive(isVisible);

        if (isVisible)
            _currencyUIInstance.transform.SetAsLastSibling();
    }

    private void SpawnLoadingUI()
    {
        if (_loadingUIPrefab == null)
        {
            Debug.LogWarning("[UIManager] LoadingUIPrefab이 연결되지 않았습니다.");
            return;
        }

        string loadingId = UIId.VeryFront.Loading;
        GameObject instance = Instantiate(_loadingUIPrefab, _veryFrontRoot);
        _createdUIDic[loadingId] = instance;
        _openedUISet.Add(loadingId);
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
                if (_createdUIDic.TryGetValue(data.Id, out GameObject existing))
                {
                    if (data.Auto)
                    {
                        existing.SetActive(true);
                        _openedUISet.Add(data.Id);
                    }
                    continue;
                }

                if (data.Auto)
                    SpawnUI(data.Id);
                else
                    SpawnUI(data.Id, hidden: true);
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