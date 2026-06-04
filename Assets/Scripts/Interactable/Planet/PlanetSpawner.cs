using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameData;

public class PlanetSpawner : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("행성 프리팹")]
    [SerializeField] private string _planetPrefabAddress = "Prefabs/Planet";

    [Header("컨테이너 설정")]
    [SerializeField] private Transform _planetContainer;

    [Header("스폰 거리 설정")]
    [SerializeField] private int _maxSpawnAttempts = 20;
    [SerializeField] private float _spawnPadding = 2f;
    [SerializeField] private float _stationExclusionRange = 3f;

    private const int MAX_GRADE = 7;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private StageData _currentStageData;
    private List<PlanetController> _spawnedPlanets;
    private Coroutine _spawnCoroutine;
    private GameObject _planetPrefab;
    private float _prefabColliderRadius;
    private CameraController _cameraController;
    private Collider2D _stationCollider;
    private int _spawnCount = 0;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _cameraController = Camera.main?.GetComponent<CameraController>();
        if (_cameraController == null)
            Debug.LogWarning("[PlanetSpawner] CameraController를 찾지 못했습니다.");

        _spawnedPlanets = new List<PlanetController>();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<string>(GameEventType.StageSelected, HandleStageSelected);
        GameEventBus.Subscribe(GameEventType.StageStartRequested, HandleStageStartRequested);
        GameEventBus.Subscribe<Transform>(GameEventType.StationSpawned, HandleStationSpawned);
        GameEventBus.Subscribe<string>(GameEventType.PlanetDestroyed, HandlePlanetDestroyed);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.StageSelected, HandleStageSelected);
        GameEventBus.Unsubscribe(GameEventType.StageStartRequested, HandleStageStartRequested);
        GameEventBus.Unsubscribe<Transform>(GameEventType.StationSpawned, HandleStationSpawned);
        GameEventBus.Unsubscribe<string>(GameEventType.PlanetDestroyed, HandlePlanetDestroyed);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStageSelected(string stageId)
    {
        _currentStageData = GameDataManager.Instance.Get<StageData>(stageId);

        if (_currentStageData == null)
            Debug.LogError($"[PlanetSpawner] 스테이지 데이터를 찾지 못했습니다: {stageId}");

        ResourceManager.Instance.LoadAsset<GameObject>(_planetPrefabAddress, OnPrefabLoaded);
    }

    private void HandleStationSpawned(Transform stationTransform)
    {
        _stationCollider = stationTransform.GetComponentInChildren<Collider2D>();
    }

    private void HandlePlanetDestroyed(string instanceId)
    {
        // 뒤 -> 앞으로 검사
        // 인덱스가 뒤로 밀리는 경우를 방지
        for (int i = _spawnedPlanets.Count - 1; i >= 0; i--)
        {
            if (_spawnedPlanets[i] == null || _spawnedPlanets[i].InstanceId == instanceId)
                _spawnedPlanets.RemoveAt(i);
        }
    }

    private void OnPrefabLoaded(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError($"[PlanetSpawner] 행성 프리팹 로드 실패: {_planetPrefabAddress}");
            return;
        }

        _planetPrefab = prefab;
        _prefabColliderRadius = SpawnCalculator.GetPrefabColliderRadius(prefab);
    }

    private void HandleStageStartRequested()
    {
        if (_currentStageData == null)
        {
            Debug.LogError("[PlanetSpawner] 스테이지 데이터가 없습니다.");
            return;
        }

        if (_planetPrefab == null)
        {
            Debug.LogError("[PlanetSpawner] 행성 프리팹이 아직 로드되지 않았습니다.");
            return;
        }

        Time.timeScale = 1f;
        StartSpawn();
    }

    public void OnExitGamePlay()
    {
        StopSpawn();
        ClearPlanets();
    }

    // =========================================================================
    // 스폰 로직
    // =========================================================================
    private void StartSpawn()
    {
        StopSpawn();
        _spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void StopSpawn()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        string[] planetIds = _currentStageData.GetPlanetList();

        if (planetIds.Length == 0)
        {
            Debug.LogError($"[PlanetSpawner] 행성 리스트가 비어있습니다.");
            yield break;
        }

        while (_spawnedPlanets.Count < _currentStageData.MaxPlanet)
        {
            string planetId = planetIds[Random.Range(0, planetIds.Length)];
            PlanetData planetData = GameDataManager.Instance.Get<PlanetData>(planetId);

            if (planetData == null)
            {
                Debug.LogWarning($"[PlanetSpawner] 행성 데이터를 찾지 못했습니다: {planetId}");
                yield return new WaitForSeconds(_currentStageData.SpawnInterval);
                continue;
            }

            SpawnPlanet(planetData);
            yield return new WaitForSeconds(_currentStageData.SpawnInterval);
        }

#if UNITY_EDITOR
        Debug.Log($"[PlanetSpawner] 최대 행성 수 도달: {_currentStageData.MaxPlanet}");
#endif
        _spawnCoroutine = null;
    }

    private void SpawnPlanet(PlanetData data)
    {
        if (GameManager.Instance.CurrentState != GameState.GamePlay) return;

        if (_planetPrefab == null)
        {
            Debug.LogError("[PlanetSpawner] 행성 프리팹이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPosition = SpawnCalculator.FindSpawnPosition(
            data.Grade, MAX_GRADE,
            _prefabColliderRadius,
            _spawnPadding,
            _stationExclusionRange,
            _maxSpawnAttempts,
            _cameraController,
            _stationCollider,
            _spawnedPlanets
        );

        GameObject instance = Instantiate(_planetPrefab, spawnPosition, Quaternion.identity);
        instance.transform.SetParent(_planetContainer);

        PlanetController planet = instance.GetComponent<PlanetController>();
        if (planet == null)
        {
            Debug.LogError("[PlanetSpawner] PlanetController 컴포넌트를 찾을 수 없습니다.");
            Destroy(instance);
            return;
        }

        string instanceId = $"{data.Id}_{_spawnCount++}";
        planet.Initialize(data, instanceId);

        _spawnedPlanets.Add(planet);
        GameEventBus.Publish(GameEventType.PlanetSpawned, instance.transform);
    }

    // =========================================================================
    // 행성 정리
    // =========================================================================
    private void ClearPlanets()
    {
        foreach (PlanetController planet in _spawnedPlanets)
        {
            if (planet != null)
                Destroy(planet.gameObject);
        }

        _spawnedPlanets.Clear();
        _spawnCount = 0;

        if (_planetContainer != null)
        {
            foreach (Transform child in _planetContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}