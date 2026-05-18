using UnityEngine;
using GameData;

// 우주선과 상호작용 가능한 오브젝트(행성, 정거장)의 공통 베이스 클래스.
public abstract class InteractableBase : MonoBehaviour
{
    protected ShipController _shipController;
    protected ShipInventory _shipInventory;

    private float _dockingSpeedThreshold;
    protected bool _isPlayerInside = false;
    private bool _isInteracting = false;

    protected virtual void OnEnable()
    {
        GameEvents.OnDataInitialized += HandleDataInitialized;
        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInitialized)
            HandleDataInitialized();
    }

    protected virtual void OnDisable()
    {
        GameEvents.OnDataInitialized -= HandleDataInitialized;
    }

    private void Update()
    {
        if (!CanInteract()) return;
        if (_shipController == null || !_isPlayerInside) return;

        bool speedOk = _shipController.IsInteractable;

        if (speedOk && !_isInteracting)
            TriggerActivate();
        else if (!speedOk && _isInteracting)
            TriggerDeactivate();
    }

    // =========================================================================
    // 트리거 감지
    // =========================================================================
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _isPlayerInside = true;
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _isPlayerInside = false;
        TriggerDeactivate();
    }

    // =========================================================================
    // 내부 흐름 제어
    // =========================================================================
    private void TriggerActivate()
    {
        _isInteracting = true;
        OnActivate();
    }

    private void TriggerDeactivate()
    {
        if (!_isInteracting) return;
        _isInteracting = false;
        OnDeactivate();
    }

    // =========================================================================
    // 하위 클래스 구현부
    // =========================================================================
    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
    protected virtual bool CanInteract() => true;

    private void HandleDataInitialized()
    {
        _dockingSpeedThreshold = GameDataManager.Instance.Constants.DockingSpeed;

        CacheShipReferences();
        OnDataInitialized();
    }

    protected virtual void OnDataInitialized() { }

    protected void CacheShipReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"[{GetType().Name}] 'Player' 태그 오브젝트를 찾지 못했습니다.");
            return;
        }

        _shipController = player.GetComponent<ShipController>();
        _shipInventory = player.GetComponent<ShipInventory>();

        if (_shipController == null)
            Debug.LogWarning($"[{GetType().Name}] ShipController를 찾지 못했습니다.");
        if (_shipInventory == null)
            Debug.LogWarning($"[{GetType().Name}] ShipInventory를 찾지 못했습니다.");
    }

    protected void CompleteInteraction()
    {
        _isInteracting = false;
    }
}