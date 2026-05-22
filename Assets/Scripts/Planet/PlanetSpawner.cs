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
    [SerializeField] private int _maxSpawnAttempts = 20;   
    [SerializeField] private float _spawnPadding = 2f;
    [SerializeField] private float _stationExclusionRange = 3f; // 정거장 경계로부터 추가 여유 간격

    private const int MAX_GRADE = 7;    // TODO: 상수로 관리, 스테이지 데이터에서 최대 등급을 가져오는 방식으로 변경 고려
    // =========================================================================
    // 내부 상태
    // =========================================================================
    private StageData _currentStageData;
    private List<PlanetController> _spawnedPlanets = new List<PlanetController>();
    private Coroutine _spawnCoroutine;
    private GameObject _planetPrefab;
    private float _prefabColliderRadius;
    private CameraController _cameraController;
    private Collider2D _stationCollider;
    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _cameraController = Camera.main?.GetComponent<CameraController>();
        if (_cameraController == null)
            Debug.LogWarning("[PlanetSpawner] CameraController를 찾지 못했습니다.");
    }

    private void OnEnable()
    {
        GameEvents.OnStageSelected += HandleStageSelected;
        GameEvents.OnStageStartRequested += HandleStageStartRequested;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnStationSpawned += HandleStationSpawned;
    }

    private void OnDisable()
    {
        GameEvents.OnStageSelected -= HandleStageSelected;
        GameEvents.OnStageStartRequested -= HandleStageStartRequested;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnStationSpawned -= HandleStationSpawned;
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

    private void OnPrefabLoaded(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError($"[PlanetSpawner] 행성 프리팹 로드 실패: {_planetPrefabAddress}");
            return;
        }

        _planetPrefab = prefab;

        Collider2D col = _planetPrefab.GetComponent<Collider2D>();
        float scale = _planetPrefab.transform.localScale.x;
        if (col is CircleCollider2D circle)
            _prefabColliderRadius = circle.radius * scale;
        else if (col is BoxCollider2D box)
            _prefabColliderRadius = Mathf.Max(box.size.x, box.size.y) * 0.5f * scale;
        else
            _prefabColliderRadius = 1f * scale;

        Debug.Log($"[PlanetSpawner] 프리팹 콜라이더 반지름: {_prefabColliderRadius}");
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

        Vector3 spawnPosition = GetSpawnPosition(data.Grade, _prefabColliderRadius);
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
        float maxDistance = GetSpawnRange(colliderRadius);
        float minDistance = colliderRadius + _stationExclusionRange;

        Debug.Log($"[스폰 영역 체크] 정거장 배제구역(최소거리): {minDistance} // 카메라 화면(최대거리): {maxDistance}");
        Debug.Log($"[카메라 시야 영역 체크] {_cameraController.ViewHalfHeight}, {_cameraController.ViewHalfWidth}");

        for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++) // 겹침 방지 루프
        {
            float gradeWeight = (float)grade / MAX_GRADE;
            float t = Mathf.Pow(Random.value, 1f - gradeWeight);

            float distance = Mathf.Lerp(minDistance, maxDistance, t);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            Vector3 candidate = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);

            if (!IsOverlapping(candidate, colliderRadius)) // 겹침 체크
                return candidate;
        }

        // 최대 시도 초과 시 경고 후 마지막 위치 반환
        Debug.LogWarning("[PlanetSpawner] 겹치지 않는 위치를 찾지 못했습니다. 마지막 위치로 스폰합니다.");
        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float fallbackDistance = Mathf.Lerp(minDistance, maxDistance, (float)grade / MAX_GRADE);
        Vector3 position = new Vector3(Mathf.Cos(fallbackAngle) * fallbackDistance, Mathf.Sin(fallbackAngle) * fallbackDistance, 0f);
        return position;
    }

    private float GetSpawnRange(float colliderRadius)
    {
        if (_cameraController != null)
            return _cameraController.ViewHalfWidth - colliderRadius - _spawnPadding;

        float defaultSize = GameDataManager.Instance.Settings.CameraHeightDefault;
        return defaultSize - colliderRadius - _spawnPadding;
    }


    private bool IsOverlapping(Vector3 candidate, float colliderRadius)
    {
        // 카메라 시야 밖 스폰은 겹침 처리
        if (_cameraController != null)
        {
            float halfW = _cameraController.ViewHalfWidth - colliderRadius - _spawnPadding;
            float halfH = _cameraController.ViewHalfHeight - colliderRadius - _spawnPadding;
            if (Mathf.Abs(candidate.x) > halfW || Mathf.Abs(candidate.y) > halfH)
                return true;
        }

        foreach (PlanetController planet in _spawnedPlanets)
        {
            if (planet == null) continue;

            Collider2D col = planet.GetComponent<Collider2D>();
            if (col == null) continue;

            float otherRadius = GetColliderRadius(col);
            float minDist = colliderRadius + otherRadius;

            if (Vector3.Distance(candidate, planet.transform.position) < minDist)
                return true;
        }

        if (_stationCollider != null)
        {
            float checkRadius = colliderRadius + _stationExclusionRange;
            Vector2 closestPoint = _stationCollider.ClosestPoint(candidate);
            if (Vector2.Distance(candidate, closestPoint) < checkRadius)
                return true;
        }

        return false;
    }

    private float GetColliderRadius(Collider2D col)
    {
        float scale = col.transform.lossyScale.x;
        if (col is CircleCollider2D circle)
            return circle.radius * scale;
        if (col is CapsuleCollider2D capsule)
            return Mathf.Min(capsule.size.x, capsule.size.y) * 0.5f * scale;
        if (col is BoxCollider2D box)
            return Mathf.Max(box.size.x, box.size.y) * 0.5f * scale;
        return col.bounds.extents.magnitude;
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