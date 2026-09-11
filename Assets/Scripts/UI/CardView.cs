using System.Collections;
using TMPro;
using Cyg.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 패(Hand)에 있는 카드 한 장의 UI 및 드래그 인터랙션을 담당한다.
///
/// <para>
/// <b>드래그 방식:</b> 카드 자체는 패에 반투명으로 유지되고,
/// 블록 모양의 고스트(BlockGhostView)가 마우스를 따라다닌다.
/// </para>
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CardView : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Inspector 직접 설정 (테스트용)")]
    [SerializeField] private CardData initialCardData;

    private GridView gridView;
    private GridManager gridManager;

    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private Image cardTypeIndicator;
    [SerializeField] private RectTransform blockPreviewRoot;
    [SerializeField, Min(1f)] private float blockPreviewTileSize = 18f;
    [SerializeField, Min(0f)] private float blockPreviewTileGap = 2f;
    [SerializeField, Range(0f, 1f)] private float blockPreviewAlpha = 1f;

    [Header("연출")]
    [Tooltip("드로우될 때 오른쪽에서 슬라이드해 들어오는 시간")]
    [SerializeField, Min(0f)] private float drawInDuration = 0.4f;
    [Tooltip("드로우될 때 시작 위치를 목표 위치보다 오른쪽으로 얼마나 띄울지")]
    [SerializeField] private float drawInOffsetX = 150f;

    // 방어/드로우 색은 "대지의 방패"(Earth), "질풍의 지혜"(Wind) 아티팩트의
    // 요구 색상(SymbolVisuals)과 동일하게 맞춘 값이다.
    private static readonly Color attackColor  = new(0.85f, 0.25f, 0.25f, 1f);
    private static readonly Color defenseColor = new(0.40f, 0.65f, 0.20f, 1f); // Earth(녹색)
    private static readonly Color drawColor    = new(0.55f, 0.90f, 0.85f, 1f); // Wind(하늘색)
    private static readonly Color weakenColor  = new(0.85f, 0.60f, 0.15f, 1f);

    // ── Runtime ──
    [Header("Runtime Debug")]
    [SerializeField] private CardData currentCardData;
    public CardData CurrentCardData => currentCardData;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private CygHandHoverAnimator handHoverAnimator;
    private bool isPlacementPhase;

    private BlockGhostView ghost;
    private int currentRotation;      // 0~3, 시계방향 90도 단위 (드래그 중 QE로 변경)
    private Vector2 lastDragScreenPos;

    // ═══════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════

    private void Awake()
    {
        rootCanvas  = GetComponentInParent<Canvas>().rootCanvas;
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = (RectTransform)transform;
        handHoverAnimator = GetComponentInParent<CygHandHoverAnimator>();

        // 프리팹 인스턴스화를 위해 런타임에 동적으로 매니저와 뷰를 탐색
        gridView = FindAnyObjectByType<GridView>();
        gridManager = FindAnyObjectByType<GridManager>();
    }

    private void OnEnable()
    {
        GameEvents.OnCombatStateChanged += HandleCombatStateChanged;
        isPlacementPhase = GameEvents.CurrentCombatState == CombatState.Placement;
    }

    private void OnDisable()
    {
        GameEvents.OnCombatStateChanged -= HandleCombatStateChanged;
    }

    private void HandleCombatStateChanged(CombatState state)
    {
        isPlacementPhase = state == CombatState.Placement;
    }

    // 손패에 새로 추가된 카드를 HandView가 생성 직후 호출한다.
    // 오른쪽에서 슬라이드해 들어오는 연출을 재생한다.
    public void PlayDrawInAnimation()
    {
        StartCoroutine(DrawInRoutine());
    }

    private IEnumerator DrawInRoutine()
    {
        // 한 프레임 대기해 CygHandHoverAnimator/레이아웃이 제자리를 잡게 한 뒤,
        // 그 자리를 목표로 오른쪽에서 슬라이드해 들어오는 연출을 재생한다.
        yield return null;

        Vector2 targetPos = rectTransform.anchoredPosition;
        Vector2 startPos = targetPos + new Vector2(drawInOffsetX, 0f);

        handHoverAnimator?.SetExternallyAnimated(transform, true);
        rectTransform.anchoredPosition = startPos;

        float elapsed = 0f;
        while (drawInDuration > 0f && elapsed < drawInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / drawInDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        handHoverAnimator?.SetExternallyAnimated(transform, false);
    }

    private void Start()
    {
        // Inspector에 SO가 연결된 경우 자동 초기화
        if (initialCardData != null)
            Setup(initialCardData);
    }

    public void Setup(CardData data)
    {
        currentCardData = data;

        if (data == null)
        {
            if (cardNameText != null) cardNameText.text = string.Empty;
            if (descriptionText != null) descriptionText.text = string.Empty;
            if (powerText != null) powerText.text = string.Empty;
            ClearBlockPreview();
            return;
        }

        if (cardNameText != null) cardNameText.text = data.CardName;
        if (descriptionText != null) descriptionText.text = data.FormattedDescription;
        if (powerText != null)    powerText.text    = data.BasePower.ToString();

        if (cardImage != null)
            cardImage.color = GetTypeColor(data.Type);

        BuildBlockPreview(data);
    }

    private static Color GetTypeColor(CardType type) => type switch
    {
        CardType.Attack            => attackColor,
        CardType.DrawNow           => drawColor,
        CardType.WeakenEnemyAttack => weakenColor,
        _                          => defenseColor,
    };

    // ═══════════════════════════════════════════
    //  Drag Handlers
    // ═══════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isPlacementPhase) return;

        handHoverAnimator?.ClearHover();

        // 카드는 패에 그대로, 반투명 처리
        canvasGroup.alpha          = 0.35f;
        canvasGroup.blocksRaycasts = false;

        currentRotation = 0;
        lastDragScreenPos = eventData.position;
        SpawnGhost(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost == null) return;

        lastDragScreenPos = eventData.position;
        ghost.UpdatePosition(eventData.position);
        RefreshPreview(eventData.position);
    }

    // 드래그 중 QE로 블록 회전
    private void Update()
    {
        if (ghost == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
            Rotate(-1);
        else if (Input.GetKeyDown(KeyCode.E)||Input.GetMouseButtonDown(1))
            Rotate(1);
    }

    private void Rotate(int delta)
    {
        currentRotation = ((currentRotation + delta) % 4 + 4) % 4;
        ghost.Rebuild(currentCardData, gridView.CellSize, currentRotation);
        RefreshPreview(lastDragScreenPos);
    }

    private void RefreshPreview(Vector2 screenPos)
    {
        var (gx, gy) = gridView.ScreenToGridCoords(screenPos);

        if (gx >= 0)
        {
            bool canPlace = gridManager.CanPlaceBlock(currentCardData, gx, gy, currentRotation);
            ghost.SetValidity(canPlace);
            gridView.ShowPreview(currentCardData, gx, gy, currentRotation);
        }
        else
        {
            ghost.SetValidity(false);
            gridView.ClearPreview();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isPlacementPhase) return;

        DestroyGhost();
        gridView.ClearPreview();

        var (gx, gy) = gridView.ScreenToGridCoords(eventData.position);
        bool placed  = gx >= 0 && gridManager.TryPlaceBlock(currentCardData, gx, gy, currentRotation);

        if (placed)
        {
            Destroy(gameObject);
        }
        else
        {
            // 배치 실패 → 카드 복원
            canvasGroup.alpha          = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    private void SpawnGhost(Vector2 startPos)
    {
        if (currentCardData == null || currentCardData.GetOccupiedCells().Length == 0) return;

        var go = new GameObject("BlockGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(BlockGhostView));
        go.transform.SetParent(rootCanvas.transform, false);

        ghost = go.GetComponent<BlockGhostView>();
        ghost.Setup(currentCardData, gridView.CellSize, currentRotation);
        ghost.UpdatePosition(startPos);
    }

    private void DestroyGhost()
    {
        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }

    private void BuildBlockPreview(CardData data)
    {
        EnsureBlockPreviewRoot();
        ClearBlockPreview();

        if (blockPreviewRoot == null)
            return;

        var occupiedCells = data.GetOccupiedCells();
        if (occupiedCells.Length == 0)
            return;

        float width = Mathf.Max(1, data.Width);
        float height = Mathf.Max(1, data.Height);
        Vector2 rootSize = blockPreviewRoot.rect.size;
        if (rootSize.x <= 0f || rootSize.y <= 0f)
            rootSize = blockPreviewRoot.sizeDelta;
        if (rootSize.x <= 0f || rootSize.y <= 0f)
            rootSize = new Vector2(100f, 100f);

        float maxTileWidth = (rootSize.x - blockPreviewTileGap * (width - 1f)) / width;
        float maxTileHeight = (rootSize.y - blockPreviewTileGap * (height - 1f)) / height;
        float tileSize = Mathf.Max(1f, Mathf.Min(blockPreviewTileSize, maxTileWidth, maxTileHeight));
        float totalWidth = width * tileSize + (width - 1f) * blockPreviewTileGap;
        float totalHeight = height * tileSize + (height - 1f) * blockPreviewTileGap;

        foreach (var (col, row, symbol) in occupiedCells)
        {
            var tile = new GameObject($"PreviewTile_{col}_{row}", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(blockPreviewRoot, false);

            var rect = tile.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(tileSize, tileSize);

            float x = -totalWidth * 0.5f + tileSize * 0.5f + col * (tileSize + blockPreviewTileGap);
            float y = totalHeight * 0.5f - tileSize * 0.5f - row * (tileSize + blockPreviewTileGap);
            rect.anchoredPosition = new Vector2(x, y);

            var image = tile.GetComponent<Image>();
            Color color = SymbolVisuals.GetColor(symbol);
            color.a = blockPreviewAlpha;
            image.color = color;
            image.raycastTarget = false;
        }
    }

    private void ClearBlockPreview()
    {
        if (blockPreviewRoot == null)
            return;

        for (int i = blockPreviewRoot.childCount - 1; i >= 0; i--)
            Destroy(blockPreviewRoot.GetChild(i).gameObject);
    }

    private void EnsureBlockPreviewRoot()
    {
        if (blockPreviewRoot != null)
            return;

        var children = GetComponentsInChildren<RectTransform>(true);
        foreach (var child in children)
        {
            if (child.name == "BlockPreviewRoot")
            {
                blockPreviewRoot = child;
                return;
            }
        }
    }
}
