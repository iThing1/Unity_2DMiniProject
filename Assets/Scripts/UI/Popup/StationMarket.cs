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
    [Header("지불 금액")]
    [SerializeField] private TMP_Text _txtLeftSmall;
    [SerializeField] private TMP_Text _txtLeftMedium;
    [SerializeField] private TMP_Text _txtLeftLarge;

    [Header("교환 대상")]
    [SerializeField] private TMP_Text _txtRightSmall;
    [SerializeField] private TMP_Text _txtRightMedium;
    [SerializeField] private TMP_Text _txtRightLarge;

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
    private List<ExchangeData> _buyList = new List<ExchangeData>();
    private List<ExchangeData> _sellList = new List<ExchangeData>();
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
        _buyList.Clear();
        _sellList.Clear();

        foreach (ExchangeData data in GameDataManager.Instance.GetAll<ExchangeData>())
        {
            if (data.Id.Contains("gold_to_ingot"))
                _buyList.Add(data);
            else if (data.Id.Contains("ingot_to_gold"))
                _sellList.Add(data);
        }

        // Id 기준 정렬
        _buyList.Sort(SortById);
        _sellList.Sort(SortById);
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
        List<ExchangeData> currentList = _isBuyMode ? _buyList : _sellList;

        TMP_Text[] leftTexts = { _txtLeftSmall, _txtLeftMedium, _txtLeftLarge };
        TMP_Text[] rightTexts = { _txtRightSmall, _txtRightMedium, _txtRightLarge };

        for (int i = 0; i < leftTexts.Length; i++)
        {
            if (i >= currentList.Count)
            {
                if (leftTexts[i] != null) leftTexts[i].text = "-";
                if (rightTexts[i] != null) rightTexts[i].text = "-";
                continue;
            }

            if (leftTexts[i] != null)
                leftTexts[i].text = currentList[i].Value.ToString("N0");
            if (rightTexts[i] != null)
                rightTexts[i].text = currentList[i].Exchanges.ToString("N0");
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
        List<ExchangeData> currentList = _isBuyMode ? _buyList : _sellList;
        ExchangeButton[] buttons = { _exchangeBtn1, _exchangeBtn2, _exchangeBtn3 };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            if (i < currentList.Count)
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].Setup(currentList[i], OnExchange);
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
        bool success = false;

        if (_isBuyMode)
        {
            // 골드 -> 주괴
            if (GameManager.Instance.TrySpendGold(data.Value))
            {
                GameManager.Instance.AddIngot(data.Exchanges);
                success = true;
            }
            else
            {
                Debug.Log("[StationMarket] 골드가 부족합니다.");
            }
        }
        else
        {
            // 주괴 -> 골드
            if (GameManager.Instance.TrySpendIngot(data.Value))
            {
                GameManager.Instance.AddGold(data.Exchanges);
                success = true;
            }
            else
            {
                Debug.Log("[StationMarket] 주괴가 부족합니다.");
            }
        }

        if (success)
            RefreshCurrencyUI();
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