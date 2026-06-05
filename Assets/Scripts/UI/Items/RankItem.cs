using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class RankItem : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("Rank")]
    [SerializeField] private Image _imgMedal;
    [SerializeField] private TMP_Text _txtRank;

    [Header("Record")]
    [SerializeField] private TMP_Text _txtClearedAt;
    [SerializeField] private TMP_Text _txtScore;

    [Header("Sprite")]
    [SerializeField] private Sprite _goldMedal;
    [SerializeField] private Sprite _silverMedal;
    [SerializeField] private Sprite _bronzeMedal;

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Setup(int rank, StageClearRecord record)
    {
        SetRank(rank);

        if (_txtClearedAt != null)
            _txtClearedAt.text = record.ClearedAt;

        if (_txtScore != null)
            _txtScore.text = record.Score.ToString("N0");
    }

    // =========================================================================
    // 내부
    // =========================================================================
    private void SetRank(int rank)
    {
        Sprite medal = GetMedalSprite(rank);
        bool isMedal = medal != null;

        if (_imgMedal != null)
        {
            _imgMedal.gameObject.SetActive(isMedal);
            if (isMedal)
                _imgMedal.sprite = medal;
        }

        if (_txtRank != null)
        {
            _txtRank.gameObject.SetActive(!isMedal);
            if (!isMedal)
                _txtRank.text = rank.ToString();
        }
    }

    private Sprite GetMedalSprite(int rank)
    {
        switch (rank)
        {
            case 1: return _goldMedal;
            case 2: return _silverMedal;
            case 3: return _bronzeMedal;
            default: return null;
        }
    }
}