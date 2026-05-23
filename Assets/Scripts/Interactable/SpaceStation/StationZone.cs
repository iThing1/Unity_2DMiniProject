using UnityEngine;

public enum StationZoneType
{
    Left,
    Center,
    Right
}

[RequireComponent(typeof(Collider2D))]
public class StationZone : MonoBehaviour
{
    [Header("구역 설정")]
    [SerializeField] private StationZoneType _zoneType;

    public StationZoneType ZoneType => _zoneType;

    public bool IsPlayerInside { get; private set; } = false;

    private StationController _station;

    private void Awake()
    {
        _station = GetComponentInParent<StationController>();

        if (_station == null)
            Debug.LogError($"[StationZone] '{gameObject.name}' : 부모에 StationController가 없습니다.");
    }

    // =========================================================================
    // 트리거 감지
    // =========================================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        IsPlayerInside = true;
        _station?.OnPlayerEnterZone(_zoneType);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        IsPlayerInside = false;
        _station?.OnPlayerExitZone(_zoneType);
    }
}