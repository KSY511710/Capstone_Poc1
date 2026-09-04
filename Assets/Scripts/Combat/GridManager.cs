using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public const int GridSize = 3;
    public const int MaxOverlapPerCell = 3;

    [Header("연출")]
    [Tooltip("블록 하나 결산 후 다음 블록까지 대기 시간")]
    [SerializeField] private float blockResolveDelay = 0.4f;

    // ── Grid State ──
    private readonly SymbolType[,] grid = new SymbolType[GridSize, GridSize];
    private readonly int[,] overlapCount = new int[GridSize, GridSize];
    private readonly List<PlacedBlock> placedBlocks = new();

    private struct PlacedBlock
    {
        public CardData card;
        public int originX, originY;
    }

    // ── Public Read-Only ──

    /// <summary> 해당 셀의 SymbolType을 반환한다. UI 미리보기 및 시각화에 활용. </summary>
    public SymbolType GetCell(int x, int y) => grid[x, y];

    /// <summary> 해당 셀에 쌓인 블록 수를 반환한다. 배율 표시에 활용. </summary>
    public int GetOverlapCount(int x, int y) => overlapCount[x, y];

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnDrawPhaseStarted       += HandleDrawPhaseStarted;
        GameEvents.OnResolutionPhaseStarted += HandleResolutionPhaseStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnDrawPhaseStarted       -= HandleDrawPhaseStarted;
        GameEvents.OnResolutionPhaseStarted -= HandleResolutionPhaseStarted;
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    public bool CanPlaceBlock(CardData card, int originX, int originY, int rotationSteps = 0)
    {
        if (card == null) return false;

        foreach (var (col, row, symbol) in card.GetOccupiedCells(rotationSteps))
        {
            int gx = originX + col;
            int gy = originY + row;

            if (gx < 0 || gx >= GridSize || gy < 0 || gy >= GridSize)
                return false;

            if (grid[gx, gy] != SymbolType.None)
            {
                if (grid[gx, gy] != symbol) return false;
                if (overlapCount[gx, gy] >= MaxOverlapPerCell) return false;
            }
        }

        return true;
    }

    public bool TryPlaceBlock(CardData card, int originX, int originY, int rotationSteps = 0)
    {
        if (card == null) return false;
        if (!CanPlaceBlock(card, originX, originY, rotationSteps)) return false;

        var cells = card.GetOccupiedCells(rotationSteps);

        // 1. 겹치는 칸을 색상별로 집계한다.
        //    CanPlaceBlock 검증상 겹친 칸의 기존 심볼은 항상 새 카드의 심볼과 같다.
        var overlapByColor = new Dictionary<SymbolType, int>();
        foreach (var (col, row, symbol) in cells)
        {
            int gx = originX + col;
            int gy = originY + row;
            if (grid[gx, gy] == SymbolType.None) continue;

            overlapByColor.TryGetValue(symbol, out int count);
            overlapByColor[symbol] = count + 1;
        }

        // 2. 그리드 업데이트
        foreach (var (col, row, symbol) in cells)
        {
            int gx = originX + col;
            int gy = originY + row;
            grid[gx, gy] = symbol;
            overlapCount[gx, gy]++;
        }

        placedBlocks.Add(new PlacedBlock { card = card, originX = originX, originY = originY });
        GameEvents.RaiseBlockPlaced(card, originX, originY);

        // 3. 색상별 겹침 카운트를 아티팩트 시스템(ArtifactManager)에 전달
        foreach (var kvp in overlapByColor)
            GameEvents.RaiseGridColorOverlapped(kvp.Key, kvp.Value);

        Debug.Log($"[GridManager] {card.CardName} 배치 완료 ({originX}, {originY})");
        return true;
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    /// <summary> UI 미리보기용 — 현재 배치 기준 합산 결과 반환. 이벤트 발행 없음. </summary>
    public ResolutionResult GetPreview() => Calculate();

    private void CalculateAndRaiseResolution()
    {
        StartCoroutine(BlockResolutionRoutine());
    }

    private IEnumerator BlockResolutionRoutine()
    {
        foreach (var pb in placedBlocks)
        {
            var result = new ResolutionResult();
            foreach (var effect in pb.card.Effects)
                EffectResolver.Apply(ref result, effect);

            Debug.Log($"[GridManager] {pb.card.CardName} 결산 — 공격 {result.damage}, 방어 {result.defense}, 회복 {result.heal}, 드로우 +{result.draw}");
            GameEvents.RaiseResolutionResult(result);

            yield return new WaitForSeconds(blockResolveDelay);
        }

        ClearGrid();
        GameEvents.RaiseResolutionComplete();
    }

    private ResolutionResult Calculate()
    {
        var result = new ResolutionResult();
        foreach (var pb in placedBlocks)
            foreach (var effect in pb.card.Effects)
                EffectResolver.Apply(ref result, effect);
        return result;
    }

    private void ClearGrid()
    {
        for (int x = 0; x < GridSize; x++)
        for (int y = 0; y < GridSize; y++)
        {
            grid[x, y] = SymbolType.None;
            overlapCount[x, y] = 0;
        }

        placedBlocks.Clear();
        Debug.Log("[GridManager] 그리드 초기화");
    }

    // ─── Event Handlers ───

    private void HandleDrawPhaseStarted(int _) => ClearGrid();
    private void HandleResolutionPhaseStarted() => CalculateAndRaiseResolution();
}
