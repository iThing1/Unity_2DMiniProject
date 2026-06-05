using System.Collections;
using UnityEngine;
using GameData;

public struct ShipStats
{
    public float BaseSpeed;
    public float BoostAcceleration;
    public float MaxFuel;
    public int Capacity;
    public float FuelRegenRate;
    public float DockingSpeedThreshold;
    public float LoaderSpeed;
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ShipStatHandler))]
public class ShipController : MonoBehaviour
{
    // =========================================================================
    // 런타임 상태 (외부 읽기 전용)
    // =========================================================================
    public float CurrentFuel { get; private set; }
    public float MaxFuel { get; private set; }
    public bool IsOverheat { get; private set; }
    public bool IsBoosting { get; private set; }
    public Vector2 Velocity => _rigidbody2D.linearVelocity;

    // GamePlay 상태일 때만 입력 허용
    private bool _isControllable = false;

    private int _lastSpeed = 0;
    // =========================================================================
    // 내부 참조
    // =========================================================================
    private Rigidbody2D _rigidbody2D;
    private ShipStatHandler _stats;
    private Vector2 _moveInput;
    private Coroutine _overheatCoroutine;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _stats = GetComponent<ShipStatHandler>();

        _rigidbody2D.gravityScale = 0f;
        _rigidbody2D.linearDamping = 0f;
        _rigidbody2D.centerOfMass = Vector2.zero;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
        GameEventBus.Subscribe<ShipStats>(GameEventType.ShipStatsChanged, HandleShipStatsChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameState, GameState>(GameEventType.GameStateChanged, HandleGameStateChanged);
        GameEventBus.Unsubscribe<ShipStats>(GameEventType.ShipStatsChanged, HandleShipStatsChanged);
    }

    // =========================================================================
    // Update / FixedUpdate
    // =========================================================================
    private void Update()
    {
        GetInput();
        HandleBoosterInput();
        HandleFuel();
        RotateTowardMovement();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        CheckSpeedThreshold();
    }

    // =========================================================================
    // 입력 관련
    // =========================================================================
    private void GetInput()
    {
        if (!_isControllable)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
    }

    private void HandleBoosterInput()
    {
        if (!_isControllable)
        {
            IsBoosting = false;
            return;
        }

        bool wantsBoost = Input.GetKey(KeyCode.Space) && _moveInput != Vector2.zero;
        bool canBoost = !IsOverheat && CurrentFuel > 0f;
        bool shouldBoost = wantsBoost && canBoost;

        if (shouldBoost == IsBoosting) return;

        IsBoosting = shouldBoost;
    }

    // =========================================================================
    // 물리 이동 (FixedUpdate)
    // =========================================================================
    private void ApplyMovement()
    {
        if (_moveInput == Vector2.zero)
        {
            _rigidbody2D.linearVelocity = Vector2.Lerp(
            _rigidbody2D.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 8f);
            _rigidbody2D.linearDamping = 0f;
            return;
        }

        _rigidbody2D.linearDamping = 0f;

        float force = IsBoosting ? _stats.BoostAcceleration : _stats.Acceleration;
        _rigidbody2D.AddForce(_moveInput * force, ForceMode2D.Force);

        float targetSpeed = IsBoosting ? _stats.BoostAcceleration : _stats.BaseSpeed;
        Vector2 targetVelocity = _moveInput * targetSpeed;
        _rigidbody2D.linearVelocity = Vector2.Lerp(
            _rigidbody2D.linearVelocity, targetVelocity, Time.fixedDeltaTime * 5f);
    }

    // =========================================================================
    // 연료 및 과열 처리 (Update)
    // =========================================================================
    private void HandleFuel()
    {
        if (IsBoosting)
        {
            ConsumeFuel(Time.deltaTime);
        }
        else if (!IsOverheat && CurrentFuel < MaxFuel)
        {
            RegenerateFuel(Time.deltaTime);
        }
    }

    private void ConsumeFuel(float deltaTime)
    {
        CurrentFuel = Mathf.Max(0f, CurrentFuel - _stats.FuelConsumeRate * deltaTime);

        if (CurrentFuel <= 0f && !IsOverheat)
            EnterOverheat();
    }

    private void RegenerateFuel(float deltaTime)
    {
        CurrentFuel = Mathf.Min(MaxFuel, CurrentFuel + _stats.FuelRegenRate * deltaTime);
    }

    // =========================================================================
    // 과열 시스템
    // =========================================================================
    private void EnterOverheat()
    {
        IsBoosting = false;
        IsOverheat = true;
        SoundManager.Instance.PlaySFX("Sounds/SFX/PowerOff");
        if (_overheatCoroutine != null)
            StopCoroutine(_overheatCoroutine);

        _overheatCoroutine = StartCoroutine(OverheatRoutine());
    }

    private IEnumerator OverheatRoutine()
    {
        yield return new WaitForSeconds(_stats.OverheatDuration);

        IsOverheat = false;
        _overheatCoroutine = null;
    }

    // =========================================================================
    // 회전 (이동 방향 바라보기)
    // =========================================================================
    private void RotateTowardMovement()
    {
        if (_moveInput == Vector2.zero) return;

        float angle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            Time.deltaTime * 15f
        );
    }

    // =========================================================================
    // 업적 체크용
    // =========================================================================
    private void CheckSpeedThreshold()
    {
        if (!_isControllable) return;

        int currentSpeed = (int)_rigidbody2D.linearVelocity.magnitude;
        if (currentSpeed <= _lastSpeed) return;

        _lastSpeed = currentSpeed;
        GameEventBus.Publish(GameEventType.SpeedReached, currentSpeed);
    }

    // =========================================================================
    // 초기화 및 이벤트 핸들러
    // =========================================================================
    public void Initialize()
    {
        _stats.LoadConstantStats();
        _stats.ApplyUpgradeStats(GameManager.Instance.SetShipStats());
        InitializeFuel();
        IsOverheat = false;
        IsBoosting = false;
        _isControllable = GameManager.Instance.CurrentState == GameState.GamePlay;
    }

    private void InitializeFuel()
    {
        MaxFuel = _stats.MaxFuel;
        CurrentFuel = MaxFuel;
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        _isControllable = next == GameState.GamePlay;

        if (next != GameState.GamePlay)
        {
            if (_overheatCoroutine != null)
            {
                StopCoroutine(_overheatCoroutine);
                _overheatCoroutine = null;
            }
            IsOverheat = false;
            IsBoosting = false;
        }
    }

    private void HandleShipStatsChanged(ShipStats stats)
    {
        float newMax = _stats.MaxFuel;
        if (Mathf.Approximately(newMax, MaxFuel)) return;

        float ratio = MaxFuel > 0f ? CurrentFuel / MaxFuel : 1f;
        MaxFuel = newMax;
        CurrentFuel = MaxFuel * ratio;
    }

    // =========================================================================
    // 외부 제어 API
    // =========================================================================
    public bool IsInteractable => _rigidbody2D.linearVelocity.magnitude <= _stats.DockingSpeedThreshold;

}