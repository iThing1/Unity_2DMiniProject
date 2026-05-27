using System.Collections.Generic;
using UnityEngine;
using GameData;

// 우주선 업그레이드 트리
// 각 업그레이드 행은 링크드리스트로 노드를 관리
// Head 노드에서 Next를 따라 순회하며 연결선 생성
public class ShipUpgrade : MonoBehaviour
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [Header("노드 프리팹")]
    [SerializeField] private GameObject _nodePrefab;
    [SerializeField] private GameObject _linePrefab;
    [SerializeField] private RectTransform _container;
    [SerializeField] private UpgradeInfo _upgradeInfo;

    [Header("레이아웃")]
    [SerializeField] private float _nodeSize = 80f;
    [SerializeField] private float _nodeSpacingX = 120f;
    [SerializeField] private float _rowSpacingY = 150f;
    [SerializeField] private Vector2 _startOffset = new Vector2(0f, 0f);

    // =========================================================================
    // 업그레이드 ID 목록 (순서 = 행 순서)
    // =========================================================================
    private static readonly string[] ShipUpgradeIds =
    {
        Upgrade.StatCargo,
        Upgrade.StatSpeed,
        Upgrade.StatAccel,
        Upgrade.StatMaxFuel,
        Upgrade.StatFuelRegen,
        Upgrade.StatDockingSpeed,
        Upgrade.StatLoaderSpeed
    };

    // =========================================================================
    // 내부 상태
    // 각 행의 헤드 노드만 보관 - 나머지는 Next로 순회
    // =========================================================================
    private readonly List<ShipUpgradeNode> _headNodes = new List<ShipUpgradeNode>();
    private readonly List<GameObject> _spawnedLines = new List<GameObject>();

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void Open()
    {
        SpawnTree();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        ClearTree();
        gameObject.SetActive(false);
    }

    // =========================================================================
    // 트리 생성
    // =========================================================================
    private void SpawnTree()
    {
        ClearTree();

        if (_nodePrefab == null || _container == null) return;

        for (int row = 0; row < ShipUpgradeIds.Length; row++)
        {
            string upgradeId = ShipUpgradeIds[row];
            UpgradeData data = GameDataManager.Instance.Get<UpgradeData>(upgradeId);
            if (data == null) continue;

            float posY = _startOffset.y + (-row * _rowSpacingY);

            // 행의 헤드 노드 (링크드리스트 시작점)
            ShipUpgradeNode headNode = null;
            ShipUpgradeNode prevNode = null;

            for (int slotLevel = 1; slotLevel <= data.MaxLevel; slotLevel++)
            {
                float posX = _startOffset.x + (slotLevel - 1) * _nodeSpacingX;

                GameObject nodeObj = Instantiate(_nodePrefab, _container);
                RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
                nodeRect.anchoredPosition = new Vector2(posX, posY);
                nodeRect.sizeDelta = new Vector2(_nodeSize, _nodeSize);

                ShipUpgradeNode node = nodeObj.GetComponent<ShipUpgradeNode>();
                if (node == null) continue;

                node.SetInfo(_upgradeInfo);
                node.Setup(upgradeId, slotLevel);

                // 링크드리스트 연결
                if (prevNode != null)
                {
                    prevNode.SetNext(node);

                    // 이전 노드 - 현재 노드 연결선 생성
                    RectTransform prevRect = prevNode.GetComponent<RectTransform>();
                    SpawnLine(prevRect, nodeRect);
                }

                if (headNode == null)
                    headNode = node;

                prevNode = node;
            }

            // 헤드 노드 보관
            if (headNode != null)
                _headNodes.Add(headNode);
        }
    }

    private void SpawnLine(RectTransform from, RectTransform to)
    {
        if (_linePrefab == null) return;

        GameObject lineObj = Instantiate(_linePrefab, _container);
        lineObj.transform.SetAsFirstSibling(); // 노드보다 뒤에 그려지도록

        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        if (lineRect != null)
        {
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = Vector2.zero;
        }

        UILineRenderer line = lineObj.GetComponent<UILineRenderer>();
        if (line == null) return;

        Vector2 nodeCenter = new Vector2(_nodeSize * 0.5f, -_nodeSize * 0.5f);
        line.SetPositions(from.anchoredPosition + nodeCenter, to.anchoredPosition + nodeCenter);
        _spawnedLines.Add(lineObj);
    }

    // =========================================================================
    // 헤드 노드에서 Next 순회로 전체 노드 파괴
    // =========================================================================
    private void ClearTree()
    {
        var toDestroy = new List<GameObject>();

        foreach (ShipUpgradeNode head in _headNodes)
        {
            ShipUpgradeNode current = head;
            while (current != null)
            {
                toDestroy.Add(current.gameObject);
                current = current.Next;
            }
        }

        foreach (GameObject obj in toDestroy)
            Destroy(obj);

        _headNodes.Clear();

        foreach (GameObject line in _spawnedLines)
        {
            if (line != null)
                Destroy(line);
        }
        _spawnedLines.Clear();
    }
}