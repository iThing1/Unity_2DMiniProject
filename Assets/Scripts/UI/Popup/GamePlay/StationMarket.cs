using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class StationMarket : UIBase
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("골드 수량 텍스트")]
    [SerializeField] private TMP_Text _txtGoldSmall;
    [SerializeField] private TMP_Text _txtGoldMedium;
    [SerializeField] private TMP_Text _txtGoldLarge;

    [Header("주괴 수량 텍스트")]
    [SerializeField] private TMP_Text _txtIngotSmall;
    [SerializeField] private TMP_Text _txtIngotMedium;
    [SerializeField] private TMP_Text _txtIngotLarge;

    [Header("탭 버튼")]
    [SerializeField] private Button _btnBuy;
    [SerializeField] private Button _btnSell;

    [Header("탭 색상")]
    [SerializeField] private Color _buyActiveColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color _sellActiveColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color _inactiveColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("교환 버튼")]
    [SerializeField] private ExchangeButton _exchangeBtn1;
    [SerializeField] private ExchangeButton _exchangeBtn2;
    [SerializeField] private ExchangeButton _exchangeBtn3;

    [Header("재화 아이콘")]
    [SerializeField] private RectTransform _imgGold;
    [SerializeField] private RectTransform _imgIngot;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private List<ExchangeData> _exchangeList = new List<ExchangeData>();
    private bool _isBuyMode = true;
    private Vector2 _goldPos;
    private Vector2 _ingotPos;

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    protected override void Awake()
    {
        base.Awake();
        if (_imgGold != null) _goldPos = _imgGold.anchoredPosition;
        if (_imgIngot != null) _ingotPos = _imgIngot.anchoredPosition;
    }

    protected override void Start()
    {
        base.Start();

        if (_btnBuy != null)
            _btnBuy.onClick.AddListener(OnClickBuy);
        if (_btnSell != null)
            _btnSell.onClick.AddListener(OnClickSell);
    }

    private void OnDestroy()
    {
        if (_btnBuy != null)
            _btnBuy.onClick.RemoveListener(OnClickBuy);
        if (_btnSell != null)
            _btnSell.onClick.RemoveListener(OnClickSell);
    }

    // =========================================================================
    // 갱신용
    // =========================================================================
    public override void Open()
    {
        base.Open();
        _isBuyMode = true;
        LoadExchangeData();
        RefreshCurrencyUI();
        RefreshTabUI();
        RefreshExchangeButtons();
    }

    // =========================================================================
    // 데이터 로드
    // =========================================================================
    private void LoadExchangeData()
    {
        _exchangeList.Clear();

        foreach (ExchangeData data in GameDataManager.Instance.GetAll<ExchangeData>())
            _exchangeList.Add(data);

        _exchangeList.Sort(SortById);
    }

    private int SortById(ExchangeData a, ExchangeData b)
    {
        return string.Compare(a.Id, b.Id);
    }

    // =========================================================================
    // UI 갱신
    // =========================================================================
    private void RefreshCurrencyUI()
    {
        TMP_Text[] goldTexts = { _txtGoldSmall, _txtGoldMedium, _txtGoldLarge };
        TMP_Text[] ingotTexts = { _txtIngotSmall, _txtIngotMedium, _txtIngotLarge };

        for (int i = 0; i < goldTexts.Length; i++)
        {
            if (i >= _exchangeList.Count)
            {
                if (goldTexts[i] != null) goldTexts[i].text = "-";
                if (ingotTexts[i] != null) ingotTexts[i].text = "-";
                continue;
            }

            if (goldTexts[i] != null)
                goldTexts[i].text = _exchangeList[i].GoldValue.ToString("N0");
            if (ingotTexts[i] != null)
                ingotTexts[i].text = _exchangeList[i].IngotValue.ToString("N0");
        }

        if (_imgGold != null)
            _imgGold.anchoredPosition = _isBuyMode ? _goldPos : _ingotPos;
        if (_imgIngot != null)
            _imgIngot.anchoredPosition = _isBuyMode ? _ingotPos : _goldPos;
    }

    private void RefreshTabUI()
    {
        // 탭 전환 시 버튼 색상 변경
        if (_btnBuy != null)
        {
            Image img = _btnBuy.GetComponent<Image>();
            if (img != null)
                img.color = _isBuyMode ? _buyActiveColor : _inactiveColor;
        }

        if (_btnSell != null)
        {
            Image img = _btnSell.GetComponent<Image>();
            if (img != null)
                img.color = _isBuyMode ? _inactiveColor : _sellActiveColor;
        }
    }

    private void RefreshExchangeButtons()
    {
        ExchangeButton[] buttons = { _exchangeBtn1, _exchangeBtn2, _exchangeBtn3 };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            if (i < _exchangeList.Count)
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].Setup(_exchangeList[i], OnExchange);
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    // =========================================================================
    // 교환 처리
    // =========================================================================
    private void OnExchange(ExchangeData data)
    {
        if (_isBuyMode)
        {
            if (CurrencyManager.Instance.TrySpendGold(data.GoldValue))
                CurrencyManager.Instance.AddIngot(data.IngotValue);
            else
                Debug.Log("[StationMarket] 골드가 부족합니다.");
        }
        else
        {
            if (CurrencyManager.Instance.TrySpendIngot(data.IngotValue))
                CurrencyManager.Instance.AddGold(data.GoldValue);
            else
                Debug.Log("[StationMarket] 주괴가 부족합니다.");
        }
    }

    // =========================================================================
    // 탭 버튼 핸들러
    // =========================================================================
    private void OnClickBuy()
    {
        if (_isBuyMode) return;
        _isBuyMode = true;
        RefreshCurrencyUI();
        RefreshTabUI();
        RefreshExchangeButtons();
    }

    private void OnClickSell()
    {
        if (!_isBuyMode) return;
        _isBuyMode = false;
        RefreshCurrencyUI();
        RefreshTabUI();
        RefreshExchangeButtons();
    }
}