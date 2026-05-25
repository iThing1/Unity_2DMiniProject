using System.Collections.Generic;
using UnityEngine;
using GameData;

public class PlanetList : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private Transform _planetListContent;
    [SerializeField] private GameObject _planetItemPrefab;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private readonly List<PlanetItem> _planetItems = new List<PlanetItem>();

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Refresh(StageData data)
    {
        ClearList();
        BuildList(data);
    }

    // =========================================================================
    // 리스트 생성/정리
    // =========================================================================
    private void ClearList()
    {
        foreach (PlanetItem item in _planetItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _planetItems.Clear();
    }

    private void BuildList(StageData data)
    {
        if (_planetItemPrefab == null || _planetListContent == null) return;

        string[] planetIds = data.GetPlanetList();

        foreach (string planetId in planetIds)
        {
            PlanetData planetData = GameDataManager.Instance.Get<PlanetData>(planetId);
            if (planetData == null) continue;

            GameObject instance = Instantiate(_planetItemPrefab, _planetListContent);
            PlanetItem item = instance.GetComponentInChildren<PlanetItem>();
            if (item == null) continue;

            item.Setup(planetData);
            _planetItems.Add(item);
        }
    }
}
