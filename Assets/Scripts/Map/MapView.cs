using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 화면 전체를 그리는 루트 뷰. GameManager.Instance.Run.currentMap을 읽어
/// 노드마다 MapNodeView를 생성/배치하고, 노드 간 연결선을 그린다.
/// CardView/GridView와 동일하게 Manager를 직접 조작하지 않고 데이터만 읽는다.
/// </summary>
public class MapView : MonoBehaviour
{
    [Header("프리팹 / 부모")]
    [Tooltip("노드 하나를 표시할 프리팹")]
    [SerializeField] private MapNodeView nodeViewPrefab;
    [Tooltip("생성된 노드 뷰들이 자식으로 들어갈 부모 Transform")]
    [SerializeField] private RectTransform nodeContainer;

    [Header("배치 간격")]
    [Tooltip("background가 지정되지 않았을 때만 쓰는 폴백 값 — 층(row) 하나당 세로 간격(px)")]
    [SerializeField] private float rowSpacing = 150f;
    [Tooltip("background가 지정되지 않았을 때만 쓰는 폴백 값 — 같은 층 안에서 컬럼 하나당 가로 간격(px)")]
    [SerializeField] private float columnSpacing = 120f;
    [Tooltip("격자 위치에서 노드를 최대 몇 px까지 흔들지 — 너무 크면 줄이 안 맞아 보이므로 10~20 권장")]
    [SerializeField] private float jitterRange = 15f;
    [Tooltip("background가 지정된 경우, 배경 가장자리에서 노드를 얼마나 안쪽으로 띄울지(px)")]
    [SerializeField] private float backgroundMargin = 80f;

    [Header("간선(연결선) 표시")]
    [Tooltip("노드 사이 연결선 두께(px)")]
    [SerializeField] private float edgeThickness = 4f;
    [Tooltip("노드 사이 연결선 색상")]
    [SerializeField] private Color edgeColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("배경")]
    [Tooltip("미리 만들어 둔 배경 이미지(RectTransform). 크기/위치/스프라이트는 에디터에서 직접 잡아두고, " +
             "런타임에는 contentRoot 밑으로 옮겨져서 노드와 같이 드래그로 움직이기만 한다.")]
    [SerializeField] private RectTransform background;

    // 노드 id → 생성된 뷰. 간선을 그릴 때 두 노드의 화면 위치를 조회하는 용도.
    private readonly Dictionary<string, MapNodeView> viewsById = new();

    // 간선 Image들이 들어갈 부모. contentRoot의 첫 번째 자식으로 만들어서
    // 이후 추가되는 노드 뷰들(형제 순서상 뒤)보다 먼저 그려지게(= 노드 아래에 깔리게) 한다.
    private RectTransform edgeContainer;

    // 실제로 드래그에 반응해 움직이는 콘텐츠 루트.
    // nodeContainer가 Canvas 자신의 RectTransform인 경우(Screen Space - Overlay)
    // Unity가 그 RectTransform을 매 프레임 화면 크기로 되돌리기 때문에 직접 드래그 대상으로 쓸 수 없다.
    // 그래서 nodeContainer 밑에 별도의 자식 RectTransform을 만들어 노드/간선은 전부 여기에 넣고, 이 콘텐츠만 움직인다.
    private RectTransform contentRoot;

    /// <summary>
    /// 씬 시작 시 현재 런의 맵을 읽어와 바로 렌더링한다.
    /// </summary>
    private void Start()
    {
        CreateContentRoot();
        CreateDragCatcher();
        BuildMap(GameManager.Instance.Run.currentMap);
    }

    /// <summary>
    /// 드래그로 실제 움직일 콘텐츠 루트를 nodeContainer의 자식으로 생성한다. 노드/간선은 전부 여기에 들어간다.
    /// </summary>
    private void CreateContentRoot()
    {
        GameObject contentObject = new GameObject("MapContent", typeof(RectTransform));
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.SetParent(nodeContainer, false);
        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;

        // 배경은 에디터에서 미리 크기/위치를 잡아둔 것을 그대로 쓴다 — contentRoot가 nodeContainer와
        // 정확히 같은 rect를 덮고 있으므로(anchor/offset 동일) worldPositionStays=false로 옮겨도
        // 화면상 위치가 그대로 유지된다. 이제부터는 노드와 함께 드래그로 움직인다.
        if (background != null)
        {
            background.SetParent(contentRoot, false);
            background.SetSiblingIndex(0);
        }
    }

    /// <summary>
    /// nodeContainer 밑에 화면 전체 크기의 투명한 드래그 캐처를 만든다.
    /// contentRoot보다 앞 순서(= 뒤에 깔림)로 넣어서, 노드 버튼 위에서는 클릭이 그대로 우선되고
    /// 버튼이 없는 빈 영역을 드래그할 때만 맵이 스크롤되게 한다.
    /// </summary>
    private void CreateDragCatcher()
    {
        GameObject catcherObject = new GameObject("MapDragCatcher", typeof(RectTransform), typeof(Image));
        RectTransform catcherRect = catcherObject.GetComponent<RectTransform>();
        catcherRect.SetParent(nodeContainer, false);
        catcherRect.SetSiblingIndex(0);
        catcherRect.anchorMin = Vector2.zero;
        catcherRect.anchorMax = Vector2.one;
        catcherRect.offsetMin = Vector2.zero;
        catcherRect.offsetMax = Vector2.zero;

        Image catcherImage = catcherObject.GetComponent<Image>();
        catcherImage.color = new Color(0f, 0f, 0f, 0f);
        catcherImage.raycastTarget = true;

        MapDragPan dragPan = catcherObject.AddComponent<MapDragPan>();
        dragPan.Initialize(contentRoot, GetComponentInParent<Canvas>());
    }

    // 노드 상태(visited/available)가 바뀔 때마다 GameManager가 발행 — 이미 생성된 노드 뷰들의 겉모습을 다시 맞춘다.
    private void OnEnable() => GameEvents.OnMapUpdated += RefreshAllNodeViews;
    private void OnDisable() => GameEvents.OnMapUpdated -= RefreshAllNodeViews;

    /// <summary>
    /// 이미 생성돼 있는 모든 노드 뷰의 겉모습(버튼 활성화 등)을 최신 상태로 다시 맞춘다.
    /// </summary>
    private void RefreshAllNodeViews()
    {
        foreach (MapNodeView view in viewsById.Values)
        {
            view.Refresh();
        }
    }

    /// <summary>
    /// 그래프 데이터를 바탕으로 노드 뷰를 전부 생성/배치한 뒤 간선을 그린다.
    /// 배치는 "격자 좌표로 먼저 정렬 → 살짝 흔들기(jitter)" 2단계로 이뤄진다:
    /// 1) row/column을 그대로 격자 위치(px)로 환산하고,
    /// 2) MapNode.jitterOffset(정규화 -1~1 값)에 jitterRange를 곱해 더해서 딱딱해 보이지 않게 한다.
    /// 격자 칸이 모두 채워지지 않는 것(일부 칸이 비어 보이는 효과)은 이 단계에서 따로 처리할 필요가 없다 —
    /// MapGenerator가 경로 기반으로 노드를 만들기 때문에 애초에 모든 (row, column) 조합에 노드가 생기지 않는다.
    /// </summary>
    /// <param name="map">렌더링할 맵 그래프</param>
    private void BuildMap(MapGraph map)
    {
        GameObject edgeContainerObject = new GameObject("EdgeContainer", typeof(RectTransform));
        edgeContainer = edgeContainerObject.GetComponent<RectTransform>();
        edgeContainer.SetParent(contentRoot, false);
        edgeContainer.SetSiblingIndex(1); // background(0) 다음, 노드들보다는 앞(=아래에 깔림)
        edgeContainer.anchorMin = Vector2.zero;
        edgeContainer.anchorMax = Vector2.one;
        edgeContainer.offsetMin = Vector2.zero;
        edgeContainer.offsetMax = Vector2.zero;

        CalculateLayout(map, out float effectiveColumnSpacing, out float effectiveRowSpacing,
            out float columnCenterOffset, out float rowOriginOffset);

        foreach (MapNode node in map.nodes)
        {
            MapNodeView view = Instantiate(nodeViewPrefab, contentRoot);
            view.Setup(node);

            // 보스는 노드가 하나뿐이고 맵의 종착점이므로, 컬럼 값과 무관하게 항상 배경 가로 중앙(x=0)에 고정한다.
            float x = node.nodeType == MapNodeType.Boss
                ? 0f
                : (node.column - columnCenterOffset) * effectiveColumnSpacing;
            Vector2 gridPosition = new Vector2(x, rowOriginOffset + node.row * effectiveRowSpacing);
            Vector2 jitterPixels = node.jitterOffset * jitterRange;
            view.GetComponent<RectTransform>().anchoredPosition = gridPosition + jitterPixels;

            viewsById[node.id] = view;
        }

        // 두 노드의 위치가 전부 정해진 뒤에야 정확한 선을 그릴 수 있으므로 노드 생성 루프 다음에 처리.
        foreach (MapEdge edge in map.edges)
        {
            DrawEdge(viewsById[edge.fromNodeId], viewsById[edge.toNodeId]);
        }

        CenterOnCurrentNode(map);
    }

    /// <summary>
    /// 씬 진입 시 플레이어가 현재 위치한 노드(currentNodeId)가 화면에 보이도록
    /// contentRoot를 세로로 스크롤해둔다. 아직 아무 노드도 선택하지 않은 새 런이면 0층 노드를 기준으로 삼는다.
    /// 가로 드래그는 지원하지 않으므로(세로 스크롤만) x는 건드리지 않는다.
    /// </summary>
    private void CenterOnCurrentNode(MapGraph map)
    {
        MapNode target = !string.IsNullOrEmpty(map.currentNodeId) ? map.GetNode(map.currentNodeId) : null;
        target ??= map.nodes.Find(n => n.row == 0);
        if (target == null || !viewsById.TryGetValue(target.id, out MapNodeView view)) return;

        float targetY = view.GetComponent<RectTransform>().anchoredPosition.y;
        Vector2 pos = contentRoot.anchoredPosition;
        pos.y = -targetY;
        contentRoot.anchoredPosition = pos;
    }

    /// <summary>
    /// background가 지정돼 있으면 그 실제 크기(rect)에 맞춰 행/열 간격을 계산해서 노드가 항상
    /// 배경 안쪽(여백 backgroundMargin만큼 뺀 영역)에 딱 맞게 퍼지도록 한다.
    /// background가 없으면 기존처럼 고정된 rowSpacing/columnSpacing을 그대로 쓴다(하위 호환).
    /// </summary>
    private void CalculateLayout(MapGraph map, out float effectiveColumnSpacing, out float effectiveRowSpacing,
        out float columnCenterOffset, out float rowOriginOffset)
    {
        effectiveColumnSpacing = columnSpacing;
        effectiveRowSpacing = rowSpacing;
        columnCenterOffset = 0f;
        rowOriginOffset = 0f;

        if (background == null || map.nodes.Count == 0) return;

        int maxRow = 0, minColumn = int.MaxValue, maxColumn = int.MinValue;
        foreach (MapNode node in map.nodes)
        {
            if (node.row > maxRow) maxRow = node.row;
            if (node.column < minColumn) minColumn = node.column;
            if (node.column > maxColumn) maxColumn = node.column;
        }

        Rect backgroundRect = background.rect;
        float usableWidth = Mathf.Max(0f, backgroundRect.width - backgroundMargin * 2f);
        float usableHeight = Mathf.Max(0f, backgroundRect.height - backgroundMargin * 2f);

        int columnSpan = Mathf.Max(1, maxColumn - minColumn);
        effectiveColumnSpacing = usableWidth / columnSpan;
        effectiveRowSpacing = maxRow > 0 ? usableHeight / maxRow : 0f;

        columnCenterOffset = (minColumn + maxColumn) * 0.5f;
        rowOriginOffset = -usableHeight * 0.5f;
    }

    /// <summary>
    /// 두 노드 뷰 사이를 잇는 연결선을 그린다.
    /// LineRenderer 대신 Image 하나를 두 지점 사이 각도/길이에 맞게 늘려서 배치한다
    /// (sprite를 지정하지 않으면 Image가 흰색 사각형으로 그려지는 UGUI 기본 동작을 이용).
    /// </summary>
    /// <param name="from">간선의 시작 노드 뷰</param>
    /// <param name="to">간선의 도착 노드 뷰</param>
    private void DrawEdge(MapNodeView from, MapNodeView to)
    {
        Vector2 fromPos = from.GetComponent<RectTransform>().anchoredPosition;
        Vector2 toPos = to.GetComponent<RectTransform>().anchoredPosition;
        Vector2 delta = toPos - fromPos;
        float distance = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        GameObject edgeObject = new GameObject("Edge", typeof(RectTransform), typeof(Image));
        RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
        edgeRect.SetParent(edgeContainer, false);
        edgeRect.anchorMin = edgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        edgeRect.pivot = new Vector2(0.5f, 0.5f);
        edgeRect.sizeDelta = new Vector2(distance, edgeThickness);
        edgeRect.anchoredPosition = fromPos + delta * 0.5f;
        edgeRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image edgeImage = edgeObject.GetComponent<Image>();
        edgeImage.color = edgeColor;
        edgeImage.raycastTarget = false;
    }
}
