using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider _sliderBGM;
    [SerializeField] private Slider _sliderSFX;

    [Header("볼륨 텍스트")]
    [SerializeField] private TMP_Text _txtBGMValue;
    [SerializeField] private TMP_Text _txtSFXValue;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Start()
    {
        base.Start();

        if (_sliderBGM != null)
            _sliderBGM.onValueChanged.AddListener(OnBGMVolumeChanged);
        if (_sliderSFX != null)
            _sliderSFX.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void OnDestroy()
    {
        if (_sliderBGM != null)
            _sliderBGM.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        if (_sliderSFX != null)
            _sliderSFX.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }

    // =========================================================================
    // 열기
    // =========================================================================
    public override void Open()
    {
        base.Open();
        RefreshSliders();
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshSliders()
    {
        if (_sliderBGM != null)
        {
            _sliderBGM.value = SoundManager.Instance.BGMVolume;
            RefreshVolumeText(_txtBGMValue, SoundManager.Instance.BGMVolume);
        }

        if (_sliderSFX != null)
        {
            _sliderSFX.value = SoundManager.Instance.SFXVolume;
            RefreshVolumeText(_txtSFXValue, SoundManager.Instance.SFXVolume);
        }
    }

    private void RefreshVolumeText(TMP_Text txt, float value)
    {
        if (txt == null) return;
        txt.text = Mathf.RoundToInt(value * 100).ToString();
    }

    // =========================================================================
    // 슬라이더 핸들러
    // =========================================================================
    private void OnBGMVolumeChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
        RefreshVolumeText(_txtBGMValue, value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        SoundManager.Instance.SetSFXVolume(value);
        RefreshVolumeText(_txtSFXValue, value);
    }
}