using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CargoInfo : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private TMP_Text _txtCargoInfo;
    [SerializeField] private TMP_Text _txtFood;
    [SerializeField] private TMP_Text _txtOre;
    [SerializeField] private Button _btnCargo;
    [SerializeField] private GameObject _cargoPanel;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Start()
    {
        if (_btnCargo != null)
            _btnCargo.onClick.AddListener(OnClickCargo);

        SetPanelOpen(false);
    }

    private void OnDestroy()
    {
        if (_btnCargo != null)
            _btnCargo.onClick.RemoveListener(OnClickCargo);
    }

    // =========================================================================
    // 초기화
    // =========================================================================
    public void Initialize()
    {
        RefreshCargo(0, 0, 0, 1);
        SetPanelOpen(false);
    }

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void RefreshCargo(int food, int ore, int total, int capacity)
    {
        if (_txtCargoInfo != null)
            _txtCargoInfo.text = $"{total} / {capacity}";

        if (_txtFood != null)
            _txtFood.text = food.ToString();

        if (_txtOre != null)
            _txtOre.text = ore.ToString();
    }

    public void SetPanelOpen(bool isOpen)
    {
        if (_cargoPanel != null)
            _cargoPanel.SetActive(isOpen);
    }

    // =========================================================================
    // 버튼 핸들러
    // =========================================================================
    private void OnClickCargo()
    {
        if (_cargoPanel == null) return;
        SetPanelOpen(!_cargoPanel.activeSelf);
    }
}
