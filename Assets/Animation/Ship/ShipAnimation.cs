using UnityEngine;

public enum ShipAnimState
{
    Idle,
    Sail,
    Boost,
    Overheat
}

// 우주선 애니메이션 상태 제어
[RequireComponent(typeof(Animator))]
public class ShipAnimation : MonoBehaviour
{
    // =========================================================================
    // 내부 참조
    // =========================================================================
    private Animator _animator;
    private ShipController _shipController;
    private ShipAnimState _currentState = ShipAnimState.Idle;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _shipController = GetComponent<ShipController>();

        if (_shipController == null)
            Debug.LogError("[ShipAnimator] ShipController를 찾지 못했습니다.");
    }

    private void Update()
    {
        if (_shipController == null) return;

        ShipAnimState newState;

        if (_shipController.IsOverheat)
            newState = ShipAnimState.Overheat;
        else if (_shipController.IsBoosting)
            newState = ShipAnimState.Boost;
        else if (!_shipController.IsInteractable)
            newState = ShipAnimState.Sail;
        else
            newState = ShipAnimState.Idle;

        SetState(newState);
    }

    // =========================================================================
    // 상태 전환
    // =========================================================================
    private void SetState(ShipAnimState newState)
    {
        if (newState == _currentState) return;

        _currentState = newState;

        switch (_currentState)
        {
            case ShipAnimState.Idle:
                ResetAllAnimParameters();
                break;
            case ShipAnimState.Sail:
                ResetAllAnimParameters();
                _animator.SetBool("isSail", true);
                break;
            case ShipAnimState.Boost:
                ResetAllAnimParameters();
                _animator.SetBool("isBoost", true);
                break;
            case ShipAnimState.Overheat:
                ResetAllAnimParameters();
                _animator.SetBool("isOverheat", true);
                break;
            default:
                ResetAllAnimParameters();
                break;
        }
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private void ResetAllAnimParameters()
    {
        _animator.SetBool("isSail", false);
        _animator.SetBool("isBoost", false);
        _animator.SetBool("isOverheat", false);
    }
}