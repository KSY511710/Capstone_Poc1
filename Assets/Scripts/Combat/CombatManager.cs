using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private CombatUnit player;
    [SerializeField] private CombatUnit enemy;

    [Header("적 설정")]
    [SerializeField] private int enemyBaseDamage = 8;
    public int EnemyBaseDamage => enemyBaseDamage;

    [Header("드로우 설정")]
    [Tooltip("전투 시작 시 첫 손패로 드로우할 장수")]
    [SerializeField] private int initialHandSize = 6;

    [Header("페이즈 딜레이")]
    [Tooltip("적 공격 전 대기")]
    [SerializeField] private float enemyAttackDelay = 1f;
    [Tooltip("적 공격 후 다음 턴까지 대기")]
    [SerializeField] private float postAttackDelay = 0.5f;

    // ── Runtime State ──
    private CombatState currentState = CombatState.None;
    public CombatState CurrentState => currentState;

    private int turnCount;
    public int TurnCount => turnCount;

    private int extraDrawNextTurn;
    private int enemyAttackReductionNextTurn;
    private bool enemyDeadAfterResolution;
    private bool resolutionComplete;

    // 다음 턴에 드로우할 기본 장수. deckManager.DrawCountPerTurn을 매번 직접 읽는 대신
    // 턴이 종료되는 시점에 이 값에 저장해두고, 다음 드로우 페이즈는 이 값을 사용한다.
    private int nextTurnDrawCount;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnTurnEndRequested       += HandleTurnEndRequested;
        GameEvents.OnResolutionResult       += HandleResolutionResult;
        GameEvents.OnResolutionComplete     += HandleResolutionComplete;
        GameEvents.OnOverlapEffectTriggered += HandleOverlapEffectTriggered;
        GameEvents.OnCardEffectTriggered    += HandleCardEffectTriggered;
        GameEvents.OnHandOverflowDamage     += HandleHandOverflowDamage;
    }

    private void OnDisable()
    {
        GameEvents.OnTurnEndRequested       -= HandleTurnEndRequested;
        GameEvents.OnResolutionResult       -= HandleResolutionResult;
        GameEvents.OnResolutionComplete     -= HandleResolutionComplete;
        GameEvents.OnOverlapEffectTriggered -= HandleOverlapEffectTriggered;
        GameEvents.OnCardEffectTriggered    -= HandleCardEffectTriggered;
        GameEvents.OnHandOverflowDamage     -= HandleHandOverflowDamage;
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    public void StartCombat()
    {
        turnCount = 0;
        deckManager.Initialize();
        nextTurnDrawCount = initialHandSize;
        GameEvents.RaiseCombatStarted();
        TransitionTo(CombatState.PlayerDraw);
    }

    // ═══════════════════════════════════════════
    //  State Machine
    // ═══════════════════════════════════════════

    private void TransitionTo(CombatState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        GameEvents.RaiseCombatStateChanged(newState);
        Debug.Log($"[CombatManager] State → {newState}");

        switch (newState)
        {
            case CombatState.PlayerDraw: EnterPlayerDraw(); break;
            case CombatState.Placement:  EnterPlacement();  break;
            case CombatState.Resolution: EnterResolution(); break;
            case CombatState.EnemyTurn:  EnterEnemyTurn();  break;
            case CombatState.Win:
            case CombatState.Lose:       EnterCombatEnd(newState == CombatState.Win); break;
        }
    }

    // ─── State Enter Methods ───

    private void EnterPlayerDraw()
    {
        turnCount++;
        player.ResetDefense();

        int drawCount = nextTurnDrawCount + extraDrawNextTurn;
        extraDrawNextTurn = 0;

        GameEvents.RaiseDrawPhaseStarted(drawCount);
        deckManager.DrawCards(drawCount);
        TransitionTo(CombatState.Placement);
    }

    private void EnterPlacement()
    {
        GameEvents.RaisePlacementPhaseStarted();
    }

    private void EnterResolution()
    {
        StartCoroutine(ResolutionRoutine());
    }

    private void EnterEnemyTurn()
    {
        StartCoroutine(EnemyTurnRoutine());
    }

    private void EnterCombatEnd(bool win)
    {
        GameEvents.RaiseCombatEnded(win);
        Debug.Log($"[CombatManager] 전투 종료 — {(win ? "승리" : "패배")}");
    }

    // ─── Coroutines ───

    private IEnumerator ResolutionRoutine()
    {
        resolutionComplete = false;
        GameEvents.RaiseResolutionPhaseStarted();

        // 블록들이 하나씩 결산 완료될 때까지 대기
        yield return new WaitUntil(() => resolutionComplete);

        if (enemyDeadAfterResolution)
            TransitionTo(CombatState.Win);
        else
            TransitionTo(CombatState.EnemyTurn);
    }

    private IEnumerator EnemyTurnRoutine()
    {
        GameEvents.RaiseEnemyTurnStarted();

        yield return new WaitForSeconds(enemyAttackDelay);

        int damage = Mathf.Max(0, enemyBaseDamage - enemyAttackReductionNextTurn);
        enemyAttackReductionNextTurn = 0;
        player.TakeDamage(damage);

        if (player.IsDead)
        {
            TransitionTo(CombatState.Lose);
            yield break;
        }

        yield return new WaitForSeconds(postAttackDelay);

        TransitionTo(CombatState.PlayerDraw);
    }

    // ─── Event Handlers ───

    private void HandleOverlapEffectTriggered(ResolutionResult result)
    {
        if (result.damage > 0)  enemy.TakeDamage(result.damage);
        if (result.defense > 0) player.AddDefense(result.defense);
        if (result.heal > 0)    player.Heal(result.heal);
        if (result.draw > 0)    extraDrawNextTurn += result.draw;
        if (result.drawNow > 0) deckManager.DrawCards(result.drawNow);
        if (result.enemyAttackReduction > 0) enemyAttackReductionNextTurn += result.enemyAttackReduction;

        if (enemy.IsDead) TransitionTo(CombatState.Win);
    }

    // 카드 배치 즉시(즉발) 발생한 효과를 상태와 무관하게 바로 반영한다.
    // HandleOverlapEffectTriggered(아티팩트 즉발)와 동일한 처리 방식이다.
    private void HandleCardEffectTriggered(ResolutionResult result)
    {
        if (result.damage > 0)  enemy.TakeDamage(result.damage);
        if (result.defense > 0) player.AddDefense(result.defense);
        if (result.heal > 0)    player.Heal(result.heal);
        if (result.draw > 0)    extraDrawNextTurn += result.draw;
        if (result.drawNow > 0) deckManager.DrawCards(result.drawNow);
        if (result.enemyAttackReduction > 0) enemyAttackReductionNextTurn += result.enemyAttackReduction;

        if (enemy.IsDead) TransitionTo(CombatState.Win);
    }

    // 손패 초과로 카드가 버려질 때마다 플레이어가 페널티 데미지를 받는다.
    private void HandleHandOverflowDamage(int damage)
    {
        player.TakeDamage(damage);

        if (player.IsDead) TransitionTo(CombatState.Lose);
    }

    private void HandleTurnEndRequested()
    {
        if (currentState != CombatState.Placement)
        {
            Debug.LogWarning("[CombatManager] 배치 페이즈가 아닌데 턴 종료 요청이 옴 — 무시");
            return;
        }

        // 턴 종료 시점에 다음 턴 드로우 장수를 결정해 저장해둔다.
        nextTurnDrawCount = deckManager.DrawCountPerTurn;

        TransitionTo(CombatState.Resolution);
    }

    // 블록 하나씩 결산 결과 수신 — 이미 배치 즉시(즉발)로 수치가 반영됐으므로
    // 여기서는 중복 적용하지 않는다. Resolution 단계 자체(연출/타이밍/UI 이벤트)는 유지.
    private void HandleResolutionResult(ResolutionResult result)
    {
        if (currentState != CombatState.Resolution) return;
    }

    // 전체 결산 완료 — 승패 기록 (손패는 더 이상 턴마다 버리지 않고 유지된다)
    private void HandleResolutionComplete()
    {
        enemyDeadAfterResolution = enemy.IsDead;
        resolutionComplete = true;
    }

    /// <summary>
    /// 이번 전투에서 사용할 적 데이터를 주입한다. 같은 Combat 씬을 재사용하면서도
    /// 일반 전투/엘리트/보스마다 다른 적(체력·공격력)으로 싸우게 하는 핵심 지점.
    /// CombatBootstrap이 StartCombat()을 호출하기 전에 먼저 호출해야 한다.
    /// encounterData가 null이면(예: 테스트로 씬을 직접 Play한 경우) 기존 Inspector 고정값을 그대로 사용한다.
    /// </summary>
    /// <param name="encounterData">RunState.pendingEncounter에서 가져온 조우 데이터</param>
    public void SetEncounter(EncounterData encounterData)
    {
        if (encounterData == null) return;  // RunState 없이 씬을 바로 테스트 플레이하는 경우 대비

        enemy.SetMaxHp(encounterData.EnemyMaxHp);
        enemyBaseDamage = encounterData.EnemyBaseDamage;
    }
}
