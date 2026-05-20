using GameData;
using UnityEngine;

public class StationSpawner : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("정거장 프리팹")]
    [SerializeField] private GameObject _stationPrefab;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private StationController _spawnedStation;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (next == GameState.GamePlay)
        {
            if (_spawnedStation == null)
                SpawnStation();
            else
                ActivateStation();

            return;
        }

        if (_spawnedStation != null)
        {
            _spawnedStation.StopSimulation();
            _spawnedStation.gameObject.SetActive(false);
        } 
    }

    // =========================================================================
    // 스폰 로직
    // =========================================================================
    private void SpawnStation()
    {
        if (_stationPrefab == null || _spawnedStation != null) return;

        GameObject instance = Instantiate(_stationPrefab, Vector3.zero, Quaternion.identity);
        _spawnedStation = instance.GetComponent<StationController>();

        if (_spawnedStation == null)
        {
            Debug.LogError("[StationSpawner] StationController 컴포넌트를 찾을 수 없습니다.");
            Destroy(instance);
            return;
        }

        _spawnedStation.Initialize();
        GameEvents.RaiseStationSpawned(instance.transform);
    }

    private void ActivateStation()
    {
        _spawnedStation.gameObject.SetActive(true);
        _spawnedStation.Initialize();
        GameEvents.RaiseStationSpawned(_spawnedStation.transform);
    }
}