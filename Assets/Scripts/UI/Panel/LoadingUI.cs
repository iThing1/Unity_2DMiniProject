using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("로딩 바")]
    [SerializeField] private Slider _progressBar;

    // =========================================================================
    // 이전 값 캐싱
    // =========================================================================
    private float _lastProgress = -1f;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Update()
    {
        if (GameDataManager.Instance == null) return;

        float progress = GameDataManager.Instance.LoadingProgress;
        if (Mathf.Approximately(progress, _lastProgress)) return;

        _lastProgress = progress;
        RefreshProgressBar(progress);
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshProgressBar(float progress)
    {
        if (_progressBar != null)
            _progressBar.value = progress;
    }
}