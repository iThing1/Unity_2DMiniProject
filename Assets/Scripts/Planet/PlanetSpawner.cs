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

    [Header("스폰 거리 설정")]
    [SerializeField] private float _minSpawnDistance = 10f;
    [SerializeField] private int _maxSpawnAttempts = 10;

    private const int MAX_GRADE = 7;    // TODO: 상수로 관리, 스테이지 데이터에서 최대 등급을 가져오는 방식으로 변경 고려
    // =========================================================================
    // 내부 상태
    // =========================================================================
    private StageData _currentStageData;
    private List<PlanetController> _spawnedPlanets = new List<PlanetController>();
    private Coroutine _spawnCoroutine;
    private GameObject _planetPrefab;
    private float _prefabColliderRadius;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnStageSelected += HandleStageSelected;
        GameEvents.OnStageStartRequested += HandleStageStartRequested;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnStageSelected -= HandleStageSelected;
        GameEvents.OnStageStartRequested -= HandleStageStartRequested;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
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

    private void OnPrefabLoaded(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError($"[PlanetSpawner] 행성 프리팹 로드 실패: {_planetPrefabAddress}");
            return;
        }

        _planetPrefab = prefab;

        Collider2D col = _planetPrefab.GetComponent<Collider2D>();
        _prefabColliderRadius = col != null ? col.bounds.extents.magnitude : 1f;
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

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (next != GameState.GamePlay)
        {
            StopSpawn();
            ClearPlanets();
        }
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
                yield return new WaitForSeconds(_currentStageData.SpawnInteval);
                continue;
            }

            SpawnPlanet(planetData);

            yield return new WaitForSeconds(_currentStageData.SpawnInteval);
        }

        Debug.Log($"[PlanetSpawner] 최대 행성 수 도달: {_currentStageData.MaxPlanet}");
        _spawnCoroutine = null;
    }

    private void SpawnPlanet(PlanetData data)
    {
        if (_planetPrefab == null)
        {
            Debug.LogError("[PlanetSpawner] 행성 프리팹이 연결되지 않았습니다.");
            return;
        }
        Collider2D prefabCollider = _planetPrefab.GetComponent<Collider2D>();
        float colliderRadius = prefabCollider != null ? prefabCollider.bounds.extents.magnitude : 1f;

        Vector3 spawnPosition = GetSpawnPosition(data.Grade, colliderRadius);

        GameObject instance = Instantiate(_planetPrefab, spawnPosition, Quaternion.identity);

        PlanetController planet = instance.GetComponent<PlanetController>();
        if (planet == null)
        {
            Debug.LogError("[PlanetSpawner] PlanetController 컴포넌트를 찾을 수 없습니다.");
            Destroy(instance);
            return;
        }

        string instanceId = $"{data.Id}_{_spawnedPlanets.Count}";
        planet.Initialize(data, instanceId);

        _spawnedPlanets.Add(planet);
        GameEvents.RaisePlanetSpawned(instance.transform);
    }

    // =========================================================================
    // 스폰 위치 계산
    // =========================================================================
    private Vector3 GetSpawnPosition(int grade, float colliderRadius)
    {
        float maxDistance = _minSpawnDistance + (_minSpawnDistance * MAX_GRADE); // 전체 스폰 범위

        for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++) // 겹침 방지 루프
        {
            float gradeWeight = (float)grade / MAX_GRADE;
            float t = Mathf.Pow(Random.value, 1f - gradeWeight);
            float distance = Mathf.Lerp(_minSpawnDistance, maxDistance, t);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 candidate = new Vector3( Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f );

            if (!IsOverlapping(candidate, colliderRadius)) // 겹침 체크
                return candidate;
        }

        // 최대 시도 초과 시 경고 후 마지막 위치 반환
        Debug.LogWarning("[PlanetSpawner] 겹치지 않는 위치를 찾지 못했습니다. 마지막 위치로 스폰합니다.");
        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float fallbackDistance = Mathf.Lerp(_minSpawnDistance, maxDistance, (float)grade / MAX_GRADE);
        Vector3 position = new Vector3(Mathf.Cos(fallbackAngle) * fallbackDistance, Mathf.Sin(fallbackAngle) * fallbackDistance, 0f);
        return position;
    }

    private bool IsOverlapping(Vector3 candidate, float colliderRadius)
    {
        foreach (PlanetController planet in _spawnedPlanets)
        {
            if (planet == null) continue;

            Collider2D col = planet.GetComponent<Collider2D>();
            if (col == null) continue;

            float otherRadius = col.bounds.extents.magnitude;
            float minDist = colliderRadius + otherRadius;

            if (Vector3.Distance(candidate, planet.transform.position) < minDist)
                return true;
        }
        return false;
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
    }
}