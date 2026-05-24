using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlanetInfo : UIBase
{
    [Header("Planet Images")]
    [SerializeField] private Image _imgPlanet;

    [Header("Planet Info")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtFood;
    [SerializeField] private TMP_Text _txtOre;
    [SerializeField] private TMP_Text _txtPop;

    private PlanetController _target;

    private float _lastFood = -1f;
    private float _lastOre = -1f;
    private float _lastPop = -1f;

    public override void Setup(object data = null)
    {
        _target = data as PlanetController;
        if (_target == null) return;

        RefreshAllUI();
    }

    protected override void OnBeforeClose()
    {
        _target = null;
    }

    // =========================================================================
    // Unity 생명주기
    // =========================================================================
    private void Update()
    {
        if (_target == null) return;

        RefreshAllUI();
    }

    // =========================================================================
    // 내부 갱신
    // =========================================================================
    private void RefreshStatic()
    {
        if (_target == null) return;

        if (_txtName != null)
            _txtName.text = _target.PlanetName;

        string[] parts = _target.PlanetSprite.Split('/');
        ResourceManager.Instance.LoadSpriteFromSheet(parts[0], parts[1], OnSpriteLoaded);
    }

    private void RefreshAllUI()
    {
        if (!Mathf.Approximately(_target.StoredFood, _lastFood))
        {
            _lastFood = _target.StoredFood;
            if (_txtFood != null)
                _txtFood.text = _lastFood.ToAbbreviatedString(0);
        }

        // 광석
        if (!Mathf.Approximately(_target.StoredOre, _lastOre))
        {
            _lastOre = _target.StoredOre;
            if (_txtOre != null)
                _txtOre.text = _lastOre.ToAbbreviatedString(0);
        }

        // 인구
        if (!Mathf.Approximately(_target.Population, _lastPop))
        {
            _lastPop = _target.Population;
            if (_txtPop != null)
                _txtPop.text = _lastPop.ToAbbreviatedString(2);
        }
        RefreshStatic();
    }

    private void OnSpriteLoaded(Sprite sprite)
    {
        if (_imgPlanet != null)
            _imgPlanet.sprite = sprite;
    }
}
