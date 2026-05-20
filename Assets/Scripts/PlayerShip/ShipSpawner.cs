using System.Collections;
using UnityEngine;
using GameData;

public class ShipSpawner : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("우주선 프리팹")]
    [SerializeField] private GameObject _shipPrefab;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private GameObject _spawnedShip;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void OnEnable()
    {
        GameEvents.OnStationSpawned += HandleStationSpawned;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnStationSpawned -= HandleStationSpawned;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleStationSpawned(Transform stationTransform)
    {
        if (_spawnedShip == null)
            SpawnShip(stationTransform);
        else
            ActivateShip(stationTransform);
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        if (next == GameState.GamePlay) return;
        if (_spawnedShip == null) return;

        _spawnedShip.SetActive(false);
    }

    // =========================================================================
    // 스폰 로직
    // =========================================================================
    private void SpawnShip(Transform stationTransform)
    {
        if (_shipPrefab == null)
        {
            Debug.LogError("[ShipSpawner] 우주선 프리팹이 연결되지 않았습니다.");
            return;
        }

        StationController station = stationTransform.GetComponent<StationController>();
        if (station == null)
        {
            Debug.LogError("[ShipSpawner] StationController 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        _spawnedShip = Instantiate(_shipPrefab);

        ShipController shipController = _spawnedShip.GetComponent<ShipController>();
        if (shipController == null)
        {
            Debug.LogError("[ShipSpawner] ShipController 컴포넌트를 찾을 수 없습니다.");
            Destroy(_spawnedShip);
            return;
        }

        station.PlacePlayerAtSpawn(_spawnedShip.transform);
        shipController.Initialize();
        StartCoroutine(RaiseShipSpawnedNextFrame());
    }

    private IEnumerator RaiseShipSpawnedNextFrame()
    {
        yield return null;
        GameEvents.RaiseShipSpawned(_spawnedShip.transform);
    }

    private void ActivateShip(Transform stationTransform)
    {
        StationController station = stationTransform.GetComponent<StationController>();
        if (station == null)
        {
            Debug.LogError("[ShipSpawner] StationController 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        _spawnedShip.SetActive(true);
        ShipController shipController = _spawnedShip.GetComponent<ShipController>();
        shipController.Initialize();

        station.PlacePlayerAtSpawn(_spawnedShip.transform);
        GameEvents.RaiseShipSpawned(_spawnedShip.transform);
    }
}