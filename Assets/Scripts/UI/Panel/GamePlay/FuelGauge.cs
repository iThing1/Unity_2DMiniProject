using UnityEngine;
using UnityEngine.UI;

public class FuelGauge : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private Slider _fuelSlider;
    [SerializeField] private Image _boostIcon;
    [SerializeField] private Sprite _boostOnSprite;
    [SerializeField] private Sprite _boostOffSprite;

    // =========================================================================
    // 초기화
    // =========================================================================
    public void Initialize()
    {
        RefreshFuel(0f, 1f);
        RefreshBooster(false);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void RefreshFuel(float current, float max)
    {
        if (_fuelSlider == null) return;
        _fuelSlider.value = max > 0f ? current / max : 0f;
    }

    public void RefreshBooster(bool isOn)
    {
        if (_boostIcon == null) return;
        Sprite target = isOn ? _boostOnSprite : _boostOffSprite;
        if (target != null)
            _boostIcon.sprite = target;
    }
}
