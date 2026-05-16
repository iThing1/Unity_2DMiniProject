using System.Collections;
using UnityEngine;
using GameData;

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
        _rigidbody2D.linearDamping = 3f;
    }

    private void OnEnable()
    {
        GameEvents.OnDataInitialized += InitializeFuel;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnUpgradeCompleted += HandleUpgradeCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnDataInitialized -= InitializeFuel;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnUpgradeCompleted -= HandleUpgradeCompleted;
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
            if (IsBoosting)
            {
                IsBoosting = false;
                GameEvents.RaiseBoosterChanged(false);
            }
            return;
        }

        bool wantsBoost = Input.GetKey(KeyCode.Space) && _moveInput != Vector2.zero;
        bool canBoost = !IsOverheat && CurrentFuel > 0f;
        bool shouldBoost = wantsBoost && canBoost;

        if (shouldBoost == IsBoosting) return;

        IsBoosting = shouldBoost;
        GameEvents.RaiseBoosterChanged(IsBoosting);
    }

    // =========================================================================
    // 물리 이동 (FixedUpdate)
    // =========================================================================
    private void ApplyMovement()
    {
        if (_moveInput == Vector2.zero) return;

        if (IsBoosting)
        {
            // 부스터 ON: AddForce 누적 → BaseSpeed 초과 가속, 상한선 없음
            _rigidbody2D.AddForce(_moveInput * _stats.Acceleration, ForceMode2D.Force);
        }
        else
        {
            // 부스터 OFF: BaseSpeed 목표로 Lerp 수렴
            Vector2 targetVelocity = _moveInput * _stats.BaseSpeed;
            _rigidbody2D.linearVelocity = Vector2.Lerp(_rigidbody2D.linearVelocity, targetVelocity, Time.fixedDeltaTime * 8f);
        }
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
        float prev = CurrentFuel;
        CurrentFuel = Mathf.Max(0f, CurrentFuel - _stats.FuelConsumeRate * deltaTime);

        if (!Mathf.Approximately(prev, CurrentFuel))
            GameEvents.RaiseFuelChanged(CurrentFuel, MaxFuel);

        if (CurrentFuel <= 0f && !IsOverheat)
            EnterOverheat();
    }

    private void RegenerateFuel(float deltaTime)
    {
        float prev = CurrentFuel;
        CurrentFuel = Mathf.Min(MaxFuel, CurrentFuel + _stats.FuelRegenRate * deltaTime);

        if (!Mathf.Approximately(prev, CurrentFuel))
            GameEvents.RaiseFuelChanged(CurrentFuel, MaxFuel);
    }
    // =========================================================================
    // 과열 시스템
    // =========================================================================
    private void EnterOverheat()
    {
        IsBoosting = false;
        GameEvents.RaiseBoosterChanged(false);

        IsOverheat = true;
        GameEvents.RaiseOverheatChanged(true);

        if (_overheatCoroutine != null)
            StopCoroutine(_overheatCoroutine);

        _overheatCoroutine = StartCoroutine(OverheatRoutine());
    }

    private IEnumerator OverheatRoutine()
    {
        yield return new WaitForSeconds(_stats.OverheatDuration);

        IsOverheat = false;
        GameEvents.RaiseOverheatChanged(false);
        _overheatCoroutine = null;
    }
    // =========================================================================
    // 회전 (이동 방향 바라보기)
    // =========================================================================
    private void RotateTowardMovement()
    {
        if (_moveInput == Vector2.zero) return;

        float angle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            Time.deltaTime * 15f
        );
    }

    // =========================================================================
    // 초기화 및 이벤트 핸들러
    // =========================================================================

    private void InitializeFuel()
    {
        MaxFuel = _stats.MaxFuel;
        CurrentFuel = MaxFuel;
        GameEvents.RaiseFuelChanged(CurrentFuel, MaxFuel);
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        _isControllable = next == GameState.GamePlay;
    }

    private void HandleUpgradeCompleted(string upgradeId, int newLevel)
    {
        if (upgradeId != "UP_Ship_MaxFuel") return;

        float newMax = _stats.MaxFuel;
        if (Mathf.Approximately(newMax, MaxFuel)) return;

        float ratio = CurrentFuel / MaxFuel;
        MaxFuel = newMax;
        CurrentFuel = MaxFuel * ratio;
        GameEvents.RaiseFuelChanged(CurrentFuel, MaxFuel);
    }

    // =========================================================================
    // 외부 제어 API
    // =========================================================================

    public bool IsInteractable => _rigidbody2D.linearVelocity.magnitude <= _stats.DockingSpeedThreshold;
    public void RefillFuel(float amount)
    {
        CurrentFuel = Mathf.Min(MaxFuel, CurrentFuel + amount);
        GameEvents.RaiseFuelChanged(CurrentFuel, MaxFuel);
    }

    public void RefillFuelFull()
    {
        CurrentFuel = MaxFuel;
        GameEvents.RaiseFuelChanged(CurrentFuel, MaxFuel);
    }
}