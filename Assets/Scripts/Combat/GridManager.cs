using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public const int GridSize = 3;
    public const int MaxOverlapPerCell = 3;

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
        GameEvents.OnDrawPhaseStarted += HandleDrawPhaseStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnDrawPhaseStarted -= HandleDrawPhaseStarted;
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
        TriggerCardEffectInstantly(card);

        // 3. 색상별 겹침 카운트를 아티팩트 시스템(ArtifactManager)에 전달
        foreach (var kvp in overlapByColor)
            GameEvents.RaiseGridColorOverlapped(kvp.Key, kvp.Value);

        Debug.Log($"[GridManager] {card.CardName} 배치 완료 ({originX}, {originY})");
        return true;
    }

    // 방금 배치된 카드의 CardEffect들을 계산해 즉시(연출 대기 없이) 결과를 발행한다.
    // ArtifactManager가 겹침 발생 시 즉발로 처리하는 것과 동일한 패턴 — 배치 순간
    // 동기적으로 EffectResolver를 돌려 그 자리에서 GameEvents로 통지한다.
    private void TriggerCardEffectInstantly(CardData card)
    {
        var result = new ResolutionResult();
        foreach (var effect in card.Effects)
            EffectResolver.Apply(ref result, effect);

        GameEvents.RaiseCardEffectTriggered(result);
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    /// <summary> UI 미리보기용 — 현재 배치 기준 합산 결과 반환. 이벤트 발행 없음. </summary>
    public ResolutionResult GetPreview() => Calculate();

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
}
