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

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly Dictionary<string, GameObject> _prefabMap = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _createdUIDic = new Dictionary<string, GameObject>();
    private readonly HashSet<string> _openedUISet = new HashSet<string>();

    public Transform VeryFrontRoot => _veryFrontRoot;
    public Transform MainRoot => _mainRoot;
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

        SpawnLoadingUI();
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
    // 외부 API
    // =========================================================================
    // 데이터 주입 없이 그냥 열기만 하면 되는 팝업
    public void OpenUI(string uiId)
    {
        if (!_createdUIDic.ContainsKey(uiId))
        {
            SpawnUI(uiId);
        }

        if (_createdUIDic.TryGetValue(uiId, out GameObject instance))
        {
            UIBase ui = instance.GetComponent<UIBase>();
            if (ui != null)
                ui.Open();
            else
                instance.SetActive(true);

            _openedUISet.Add(uiId);
        }
    }

    // 이미 열려있는 UI의 실시간 갱신
    public T GetUI<T>(string uiId) where T : MonoBehaviour
    {
        if (!_createdUIDic.TryGetValue(uiId, out GameObject panel)) return null;
        return panel.GetComponent<T>();
    }

    // 열기 전에 데이터를 먼저 넣어야 하는 팝업
    public T PrepareUI<T>(string uiId) where T : MonoBehaviour
    {
        if (!_createdUIDic.ContainsKey(uiId))
        {
            SpawnUI(uiId);
        }

        return GetUI<T>(uiId);
    }

    // 닫기
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

    // 열려있는지 확인용
    public bool IsOpen(string uiId)
    {
        return _openedUISet.Contains(uiId);
    }

    // =========================================================================
    // 스폰 관련
    // =========================================================================
    private void SpawnUI(string uiId)
    {
        if (!_prefabMap.TryGetValue(uiId, out GameObject prefab) || prefab == null) return;

        UIData data = GameDataManager.Instance.Get<UIData>(uiId);
        if (data == null) return;

        Transform root = GetRootTransform(data.Type);
        GameObject instance = Instantiate(prefab, root);

        instance.SetActive(false);
        _createdUIDic[uiId] = instance;
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
}