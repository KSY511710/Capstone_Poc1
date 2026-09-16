using System;

/// <summary>
/// 글로벌 이벤트 버스 — 시스템 간 결합도를 최소화하기 위한 정적 이벤트 허브.
/// 
/// <para>
/// <b>설계 원칙:</b>
/// <list type="bullet">
///   <item>모듈 간 직접 참조를 피하고, 이벤트 구독/발행으로만 통신한다.</item>
///   <item>이벤트 인자는 가능한 한 값 타입(struct) 또는 불변 객체를 사용한다.</item>
///   <item>구독 해제 누락을 방지하기 위해 OnDestroy에서 반드시 -= 해야 한다.</item>
/// </list>
/// </para>
/// </summary>
public static class GameEvents
{
    // ─── 전투 상태 ───
    /// <summary> 전투 시작 시 발행 </summary>
    public static event Action OnCombatStarted;
    /// <summary> 전투 종료 시 발행 (승리/패배 결과 포함) </summary>
    public static event Action<bool> OnCombatEnded;  // true = win, false = lose
    /// <summary> 전투 상태 전이 시 발행 (새 상태) </summary>
    public static event Action<CombatState> OnCombatStateChanged;
    public static CombatState CurrentCombatState { get; private set; }

    // ─── 턴 흐름 ───
    /// <summary> 플레이어 드로우 페이즈 시작 </summary>
    public static event Action<int> OnDrawPhaseStarted;  // drawCount
    /// <summary> 배치 페이즈 시작 </summary>
    public static event Action OnPlacementPhaseStarted;
    /// <summary> 플레이어가 턴 종료 버튼을 누름 </summary>
    public static event Action OnTurnEndRequested;
    /// <summary> 결산 페이즈 시작 </summary>
    public static event Action OnResolutionPhaseStarted;
    /// <summary> 적 턴 시작 </summary>
    public static event Action OnEnemyTurnStarted;

    // ─── 덱 & 카드 ───
    /// <summary> 카드가 드로우되었을 때 (드로우된 카드 데이터) </summary>
    public static event Action<CardData> OnCardDrawn;
    /// <summary> 카드가 핸드에서 제거(사용)되었을 때 </summary>
    public static event Action<CardData> OnCardUsed;
    /// <summary> 손패 초과로 카드가 버려져 페널티 데미지가 발생했을 때 (데미지량) </summary>
    public static event Action<int> OnHandOverflowDamage;

    // ─── 그리드 & 블록 배치 ───
    /// <summary> 블록이 그리드에 배치되었을 때 (카드 데이터, 그리드 좌표) </summary>
    public static event Action<CardData, int, int> OnBlockPlaced;  // cardData, gridX, gridY

    // ─── 결산 ───
    /// <summary> 블록 하나의 결산 결과 발행 (블록마다 순서대로 호출됨) </summary>
    public static event Action<ResolutionResult> OnResolutionResult;
    /// <summary> 모든 블록 결산 완료 신호 — 손패 버리기·전이 타이밍용 </summary>
    public static event Action OnResolutionComplete;
    /// <summary> 블록 겹침 시 즉시 발동하는 효과 발행 (아티팩트 발동 결과) </summary>
    public static event Action<ResolutionResult> OnOverlapEffectTriggered;
    /// <summary> 카드를 배치하는 즉시(연출 대기 없이) 발동하는 카드 효과 결과 </summary>
    public static event Action<ResolutionResult> OnCardEffectTriggered;
    /// <summary> 그리드 배치 중 특정 색상이 겹쳤을 때 발행 (겹친 색, 겹친 칸 수) — 아티팩트 진행도 누적용 </summary>
    public static event Action<SymbolType, int> OnGridColorOverlapped;
    /// <summary> 아티팩트의 색상별 진행도가 바뀌었을 때 발행 (아티팩트, 색상, 현재 카운트, 요구 카운트) — UI 표시용 </summary>
    public static event Action<ArtifactData, SymbolType, int, int> OnArtifactProgressChanged;
    /// <summary> 적에게 데미지가 적용되었을 때 (적용된 데미지) </summary>
    public static event Action<int> OnDamageDealtToEnemy;
    /// <summary> 플레이어에게 데미지가 적용되었을 때 (적용된 데미지) </summary>
    public static event Action<int> OnDamageDealtToPlayer;
    /// <summary> 플레이어 방어도에 흡수된 데미지 </summary>
    public static event Action<int> OnDamageAbsorbedByPlayer;

    // ─── 체력 변동 ───
    /// <summary> 플레이어 HP 변경 시 (현재HP, 최대HP) </summary>
    public static event Action<int, int> OnPlayerHpChanged;
    /// <summary> 적 HP 변경 시 (현재HP, 최대HP) </summary>
    public static event Action<int, int> OnEnemyHpChanged;
    /// <summary> 플레이어 방어도 변경 시 (현재 방어도) </summary>
    public static event Action<int> OnPlayerDefenseChanged;

    // ─── 맵 ───
    /// <summary> 플레이어가 맵에서 노드를 클릭해 선택했을 때 발행 (선택된 노드) </summary>
    public static event Action<MapNode> OnMapNodeSelected;
    /// <summary> 노드의 visited/available 상태가 바뀌어 맵 UI를 다시 그려야 할 때 발행 </summary>
    public static event Action OnMapUpdated;

    // ═══════════════════════════════════════════
    //  Invoke Methods (이벤트 발행 전용)
    // ═══════════════════════════════════════════

    public static void RaiseCombatStarted() => OnCombatStarted?.Invoke();
    public static void RaiseCombatEnded(bool win) => OnCombatEnded?.Invoke(win);
    public static void RaiseCombatStateChanged(CombatState state)
    {
        CurrentCombatState = state;
        OnCombatStateChanged?.Invoke(state);
    }

    public static void RaiseDrawPhaseStarted(int drawCount) => OnDrawPhaseStarted?.Invoke(drawCount);
    public static void RaisePlacementPhaseStarted() => OnPlacementPhaseStarted?.Invoke();
    public static void RaiseTurnEndRequested() => OnTurnEndRequested?.Invoke();
    public static void RaiseResolutionPhaseStarted() => OnResolutionPhaseStarted?.Invoke();
    public static void RaiseEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();

    public static void RaiseCardDrawn(CardData card) => OnCardDrawn?.Invoke(card);
    public static void RaiseCardUsed(CardData card) => OnCardUsed?.Invoke(card);
    public static void RaiseHandOverflowDamage(int damage) => OnHandOverflowDamage?.Invoke(damage);

    public static void RaiseBlockPlaced(CardData card, int x, int y) => OnBlockPlaced?.Invoke(card, x, y);

    public static void RaiseResolutionResult(ResolutionResult result) => OnResolutionResult?.Invoke(result);
    public static void RaiseResolutionComplete() => OnResolutionComplete?.Invoke();
    public static void RaiseOverlapEffectTriggered(ResolutionResult result) => OnOverlapEffectTriggered?.Invoke(result);
    public static void RaiseCardEffectTriggered(ResolutionResult result) => OnCardEffectTriggered?.Invoke(result);
    public static void RaiseGridColorOverlapped(SymbolType color, int count) => OnGridColorOverlapped?.Invoke(color, count);
    public static void RaiseArtifactProgressChanged(ArtifactData artifact, SymbolType color, int current, int required) => OnArtifactProgressChanged?.Invoke(artifact, color, current, required);
    public static void RaiseDamageDealtToEnemy(int damage) => OnDamageDealtToEnemy?.Invoke(damage);
    public static void RaiseDamageDealtToPlayer(int damage) => OnDamageDealtToPlayer?.Invoke(damage);
    public static void RaiseDamageAbsorbedByPlayer(int absorbed) => OnDamageAbsorbedByPlayer?.Invoke(absorbed);

    public static void RaisePlayerHpChanged(int current, int max) => OnPlayerHpChanged?.Invoke(current, max);
    public static void RaiseEnemyHpChanged(int current, int max) => OnEnemyHpChanged?.Invoke(current, max);
    public static void RaisePlayerDefenseChanged(int defense) => OnPlayerDefenseChanged?.Invoke(defense);

    public static void RaiseMapNodeSelected(MapNode node) => OnMapNodeSelected?.Invoke(node);
    public static void RaiseMapUpdated() => OnMapUpdated?.Invoke();

    /// <summary>
    /// 모든 이벤트 구독을 해제한다.
    /// 씬 전환 시 호출하여 좀비 리스너를 방지한다.
    /// </summary>
    public static void ClearAll()
    {
        OnCombatStarted = null;
        OnCombatEnded = null;
        OnCombatStateChanged = null;
        OnDrawPhaseStarted = null;
        OnPlacementPhaseStarted = null;
        OnTurnEndRequested = null;
        OnResolutionPhaseStarted = null;
        OnEnemyTurnStarted = null;
        OnCardDrawn = null;
        OnCardUsed = null;
        OnHandOverflowDamage = null;
        OnBlockPlaced = null;
        OnResolutionResult = null;
        OnResolutionComplete = null;
        OnOverlapEffectTriggered = null;
        OnCardEffectTriggered = null;
        OnGridColorOverlapped = null;
        OnArtifactProgressChanged = null;
        OnDamageDealtToEnemy = null;
        OnDamageDealtToPlayer = null;
        OnDamageAbsorbedByPlayer = null;
        OnPlayerHpChanged = null;
        OnEnemyHpChanged = null;
        OnPlayerDefenseChanged = null;
        OnMapNodeSelected = null;
        OnMapUpdated = null;
    }
}
