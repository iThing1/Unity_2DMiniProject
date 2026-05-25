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

    public void OnEnterGamePlay()
    {
        if (_spawnedStation == null)
            SpawnStation();
        else
            ActivateStation();
    }

    public void OnExitGamePlay()
    {
        if (_spawnedStation == null) return;

        _spawnedStation.StopSimulation();
        _spawnedStation.gameObject.SetActive(false);
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
        GameEventBus.Publish(GameEventType.StationSpawned, instance.transform);
    }

    private void ActivateStation()
    {
        _spawnedStation.gameObject.SetActive(true);
        _spawnedStation.Initialize();
        GameEventBus.Publish(GameEventType.StationSpawned, _spawnedStation.transform);
    }
}