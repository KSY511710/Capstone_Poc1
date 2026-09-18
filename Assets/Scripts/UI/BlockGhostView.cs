using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중 블록 모양을 마우스 위치에 시각화하는 고스트(Ghost) 컴포넌트.
/// CardView가 드래그 시작 시 동적으로 생성하고 종료 시 파괴한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BlockGhostView : MonoBehaviour
{
    private Image[] tiles;

    // ═══════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════

    /// <summary>
    /// 블록 형태에 맞게 타일을 생성한다.
    /// </summary>
    /// <param name="card">카드 데이터 (블록 형태 포함)</param>
    /// <param name="cellSize">그리드 셀 한 칸의 픽셀 크기</param>
    /// <param name="rotationSteps">시계방향 90도 단위 회전 횟수</param>
    public void Setup(CardData card, float cellSize, int rotationSteps = 0)
    {
        // 레이캐스트를 통과시켜 그리드 셀 감지가 막히지 않도록
        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        BuildTiles(card, cellSize, rotationSteps);
    }

    /// <summary> 회전(QE) 등으로 모양이 바뀌었을 때 타일을 다시 만든다. </summary>
    public void Rebuild(CardData card, float cellSize, int rotationSteps)
    {
        BuildTiles(card, cellSize, rotationSteps);
    }

    private void BuildTiles(CardData card, float cellSize, int rotationSteps)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        var occupied = card.GetOccupiedCells(rotationSteps);
        tiles = new Image[occupied.Length];

        float tileSize = cellSize - 6f;

        for (int i = 0; i < occupied.Length; i++)
        {
            var (col, row, symbol) = occupied[i];

            var tileGo = new GameObject($"Tile_{col}_{row}", typeof(RectTransform), typeof(Image));
            tileGo.transform.SetParent(transform, false);

            var rt = tileGo.GetComponent<RectTransform>();
            rt.sizeDelta        = Vector2.one * tileSize;
            rt.anchoredPosition = new Vector2(col * cellSize, -row * cellSize);

            var image = tileGo.GetComponent<Image>();
            Color color = SymbolVisuals.GetColor(symbol);
            color.a = 0.70f;
            image.color = color;

            tiles[i] = image;
        }
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    /// <summary> 고스트를 해당 화면 좌표로 이동한다. </summary>
    public void UpdatePosition(Vector2 screenPos)
    {
        transform.position = screenPos;
    }
}
