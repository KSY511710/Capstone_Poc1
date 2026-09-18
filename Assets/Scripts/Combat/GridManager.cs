using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public const int GridSize = 3;
    public const int MaxOverlapPerCell = 3;

    // ── Grid State ──
    private readonly SymbolType[,] grid = new SymbolType[GridSize, GridSize];
    private readonly int[,] overlapCount = new int[GridSize, GridSize];

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
        GameEvents.OnEnemyTurnStarted += HandleEnemyTurnStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyTurnStarted -= HandleEnemyTurnStarted;
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

            // 같은 색이 3번 겹치면 그 칸은 즉시 비워지므로(TryPlaceBlock 참고),
            // 배치 시점에 overlapCount가 MaxOverlapPerCell 이상으로 남아있는 경우는 없다.
            if (grid[gx, gy] != SymbolType.None && grid[gx, gy] != symbol)
                return false;
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

        // 2. 그리드 업데이트 — 같은 색이 3번째로 겹친 칸은 즉시 비워진다(팝).
        var poppedColors = new List<SymbolType>();
        foreach (var (col, row, symbol) in cells)
        {
            int gx = originX + col;
            int gy = originY + row;
            grid[gx, gy] = symbol;
            overlapCount[gx, gy]++;

            if (overlapCount[gx, gy] >= MaxOverlapPerCell)
            {
                grid[gx, gy] = SymbolType.None;
                overlapCount[gx, gy] = 0;
                poppedColors.Add(symbol);
            }
        }

        GameEvents.RaiseBlockPlaced(card, originX, originY);

        // 3. 카드 효과를 배치 즉시 결산하여 발행
        var result = new ResolutionResult();
        foreach (var effect in card.Effects)
            EffectResolver.Apply(ref result, effect);

        Debug.Log($"[GridManager] {card.CardName} 배치 즉시 결산 — 공격 {result.damage}, 방어 {result.defense}, 회복 {result.heal}, 드로우 +{result.draw}");
        GameEvents.RaiseResolutionResult(result);

        // 4. 색상별 겹침 카운트를 아티팩트 시스템(ArtifactManager)에 전달
        foreach (var kvp in overlapByColor)
            GameEvents.RaiseGridColorOverlapped(kvp.Key, kvp.Value);

        // 5. 3겹으로 팝된 칸은 해당 색 아티팩트 진행도에 추가 보너스 +1
        foreach (var color in poppedColors)
        {
            Debug.Log($"[GridManager] {color} 칸 3겹 팝 — 아티팩트 진행도 +1 보너스");
            GameEvents.RaiseGridColorOverlapped(color, 1);
        }

        Debug.Log($"[GridManager] {card.CardName} 배치 완료 ({originX}, {originY})");
        return true;
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    private void ClearGrid()
    {
        for (int x = 0; x < GridSize; x++)
        for (int y = 0; y < GridSize; y++)
        {
            grid[x, y] = SymbolType.None;
            overlapCount[x, y] = 0;
        }

        Debug.Log("[GridManager] 그리드 초기화");
    }

    // ─── Event Handlers ───

    // 그리드는 적 턴이 시작되는 시점에 정리한다 — 턴 종료 직후~버리기 페이즈 동안은
    // 이번 턴에 배치한 블록이 그대로 보인다.
    private void HandleEnemyTurnStarted() => ClearGrid();
}
