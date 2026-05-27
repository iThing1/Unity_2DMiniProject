using UnityEngine;
using UnityEngine.UI;

public class StationTimer : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("프로그레스바")]
    [SerializeField] private Slider _farmProgressBar;
    [SerializeField] private Slider _refineProgressBar;

    private StationSimulator _simulator;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Update()
    {
        if (_simulator == null) return;

        UpdateBar();
    }

    public void Initialize(StationSimulator simulator)
    {
        _simulator = simulator;
    }

    // =========================================================================
    // 진행바 갱신
    // =========================================================================
    private void UpdateBar()
    {
        if (_farmProgressBar != null)
            _farmProgressBar.value = _simulator.FarmProgress;

        if (_refineProgressBar != null)
        {
            _refineProgressBar.value = _simulator.IsRefining 
                 ? _simulator.RefineProgress
                 : 0f;
        }
    }

}
