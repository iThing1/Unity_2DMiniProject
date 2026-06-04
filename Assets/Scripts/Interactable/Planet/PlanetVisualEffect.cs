using UnityEngine;

// 행성 번영도 시각 효과 전담 컴포넌트
public class PlanetVisualEffect : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("색상")]
    [SerializeField] private Color _veryProsperousColor = new Color(1.0f, 1.0f, 1.0f); 
    [SerializeField] private Color _prosperousColor = new Color(0.9f, 1.0f, 0.9f);
    [SerializeField] private Color _neutralColor = new Color(0.85f, 0.85f, 0.7f);
    [SerializeField] private Color _poorColor = new Color(0.7f, 0.6f, 0.5f);
    [SerializeField] private Color _criticalColor = new Color(0.5f, 0.3f, 0.3f);

    [Header("전환 속도")]
    [SerializeField] private float _colorTransitionSpeed = 1.5f;

    [Header("Critical 펄스")]
    [SerializeField] private float _pulseSpeed = 2.5f;
    [SerializeField] private float _pulseIntensity = 0.2f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private SpriteRenderer _spriteRenderer;
    private PlanetSimulator _simulator;

    private Color _targetColor;
    private PlanetState _lastState = (PlanetState)(-1);

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Initialize(PlanetSimulator simulator)
    {
        _simulator = simulator;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _targetColor = GetColorForState(_simulator.State);
        if (_spriteRenderer != null)
            _spriteRenderer.color = _targetColor;
    }

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Update()
    {
        if (_simulator == null || _spriteRenderer == null) return;

        PlanetState state = _simulator.State;

        if (state != _lastState)
        {
            _lastState = state;
            _targetColor = GetColorForState(state);
        }

        if (state == PlanetState.Critical || state == PlanetState.Destroyed)
            ApplyCriticalPulse();
        else
            ApplyColorTransition();
    }

    // =========================================================================
    // 색상 전환
    // =========================================================================
    private void ApplyColorTransition()
    {
        _spriteRenderer.color = Color.Lerp(
            _spriteRenderer.color,
            _targetColor,
            Time.deltaTime * _colorTransitionSpeed
        );
    }

    private void ApplyCriticalPulse()
    {
        float pulse = Mathf.Sin(Time.time * _pulseSpeed) * _pulseIntensity;
        Color pulseColor = new Color(
            _targetColor.r + pulse,
            _targetColor.g,
            _targetColor.b,
            _targetColor.a
        );
        _spriteRenderer.color = Color.Lerp(
            _spriteRenderer.color,
            pulseColor,
            Time.deltaTime * _colorTransitionSpeed
        );
    }

    // =========================================================================
    // 내부 유틸
    // =========================================================================
    private Color GetColorForState(PlanetState state)
    {
        switch (state)
        {
            case PlanetState.VeryProsperous: return _veryProsperousColor;
            case PlanetState.Prosperous: return _prosperousColor;
            case PlanetState.Neutral: return _neutralColor;
            case PlanetState.Poor: return _poorColor;
            case PlanetState.Critical: return _criticalColor;
            case PlanetState.Destroyed: return _criticalColor;
            default: return Color.white;
        }
    }
}