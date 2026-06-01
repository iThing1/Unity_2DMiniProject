using GameData;
using UnityEngine;
using UnityEngine.UI;

public class PlanetProgressBar : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("Progress Bar Settings")]
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fillImage;

    [Header("Prosperty Images")]
    [SerializeField] private Color _veryProsperousColor = new Color(0.4f, 0.9f, 1.0f);   // 밝은 파랑;    
    [SerializeField] private Color _prosperousColor = new Color(0.2f, 0.5f, 1.0f);   // 파랑
    [SerializeField] private Color _neutralColor = new Color(0.2f, 0.8f, 0.2f);   // 초록
    [SerializeField] private Color _poorColor = new Color(1.0f, 0.8f, 0.0f);   // 노랑
    [SerializeField] private Color _criticalColor = new Color(1.0f, 0.2f, 0.2f);   // 빨강

    private PlanetSimulator _simulator;

    // 이전 상태 캐싱용
    private float _lastProgress = -1f;
    private PlanetState _lastState = (PlanetState)(-1);

    // =========================================================================
    // Unity 생명주기
    // =========================================================================

    private void Update()
    {
        if (_simulator == null) return;

        // 사이클 진행도 갱신
        float progress = _simulator.CycleProgress;
        if (!Mathf.Approximately(progress, _lastProgress))
        {
            _lastProgress = progress;
            RefreshSlider(progress);
        }

        // 번영도별 색상 갱신
        PlanetState state = _simulator.State;
        if (state != _lastState)
        {
            _lastState = state;
            RefreshColor(state);
        }  
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    public void Initialize(PlanetSimulator simulator)
    {
        _simulator = simulator;

        _lastProgress = -1f;
        _lastState = (PlanetState)(-1);
    }


    // =========================================================================
    // UI 갱신용
    // =========================================================================
    private void RefreshSlider(float progress)
    {
        if (_slider == null) return;
        _slider.value = progress;
    }

    private void RefreshColor(PlanetState state)
    {
        if (_fillImage == null) return;
        _fillImage.color = GetColorForState(state);
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
            default: return Color.grey;
        }
    }
}
