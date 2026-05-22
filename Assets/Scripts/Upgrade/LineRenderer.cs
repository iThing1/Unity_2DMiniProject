using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class LineRenderer : Graphic
{
    // =========================================================================
    // Inspector 연결
    // =========================================================================
    [SerializeField] private float _lineWidth = 4f;

    // =========================================================================
    // 내부 상태
    // =========================================================================
    private Vector2 _startPos;
    private Vector2 _endPos;

    // =========================================================================
    // 외부 API
    // =========================================================================
    public void SetPositions(Vector2 startPos, Vector2 endPos)
    {
        _startPos = startPos;
        _endPos = endPos;
        SetVerticesDirty();
    }

    public void SetLineWidth(float width)
    {
        _lineWidth = width;
        SetVerticesDirty();
    }

    // =========================================================================
    // 메시 생성
    // =========================================================================
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Vector2 dir = (_endPos - _startPos).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (_lineWidth * 0.5f);

        UIVertex vertex = new UIVertex();
        vertex.color = color;

        // 사각형 4꼭짓점으로 선 표현
        vertex.position = _startPos + perp;
        vh.AddVert(vertex);

        vertex.position = _startPos - perp;
        vh.AddVert(vertex);

        vertex.position = _endPos - perp;
        vh.AddVert(vertex);

        vertex.position = _endPos + perp;
        vh.AddVert(vertex);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
