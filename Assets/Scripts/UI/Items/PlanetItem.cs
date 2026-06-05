using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class PlanetItem : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private Image _imgPlanet;
    [SerializeField] private TMP_Text _txtName;

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Setup(PlanetData data)
    {
        if (data == null) return;

        if (_txtName != null)
            _txtName.text = data.Name;

        if (_imgPlanet != null)
        {
            string[] parts = data.PlanetSprite.Split('/');
            if (parts.Length == 2)
                ResourceManager.Instance.LoadSpriteFromSheet(parts[0], parts[1], OnSpriteLoaded);
            else
                Debug.LogWarning($"[PlanetItem] PlanetSprite 경로 형식이 올바르지 않습니다: {data.PlanetSprite}");
        }
    }

    // =========================================================================
    // 내부 콜백
    // =========================================================================
    private void OnSpriteLoaded(Sprite sprite)
    {
        if (_imgPlanet == null) return;
        if (sprite == null)
        {
            Debug.LogWarning($"[PlanetItem] 스프라이트 로드 실패");
            return;
        }

        _imgPlanet.sprite = sprite;
    }
}