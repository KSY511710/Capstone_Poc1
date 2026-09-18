using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private int enemyDefenseAmount = 8;
    public int EnemyDefenseAmount => enemyDefenseAmount;

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
    private bool enemyAttackTurn = true; // 적은 공격 → 방어 순서로 번갈아 행동한다

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnTurnEndRequested       += HandleTurnEndRequested;
        GameEvents.OnResolutionResult       += HandleResolutionResult;
        GameEvents.OnOverlapEffectTriggered += HandleOverlapEffectTriggered;
        GameEvents.OnDiscardConfirmed       += HandleDiscardConfirmed;
    }

    private void OnDisable()
    {
        GameEvents.OnTurnEndRequested       -= HandleTurnEndRequested;
        GameEvents.OnResolutionResult       -= HandleResolutionResult;
        GameEvents.OnOverlapEffectTriggered -= HandleOverlapEffectTriggered;
        GameEvents.OnDiscardConfirmed       -= HandleDiscardConfirmed;
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    public void StartCombat()
    {
        turnCount = 0;
        enemyAttackTurn = true;
        deckManager.Initialize();
        GameEvents.RaiseCombatStarted();
        GameEvents.RaiseEnemyIntentChanged(GetCurrentIntent());
        TransitionTo(CombatState.PlayerDraw);
    }

    private EnemyIntent GetCurrentIntent()
    {
        return enemyAttackTurn
            ? new EnemyIntent(EnemyIntentType.Attack, enemyBaseDamage)
            : new EnemyIntent(EnemyIntentType.Defense, enemyDefenseAmount);
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
            case CombatState.Discard:    EnterDiscard();    break;
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

        // 이번 턴 목표 손패 수 = 기본 목표(7) + "다음 턴 추가 드로우" 효과 보너스
        int turnTargetHandSize = deckManager.TargetHandSize + extraDrawNextTurn;
        extraDrawNextTurn = 0;

        int drawCount = Mathf.Max(0, turnTargetHandSize - deckManager.Hand.Count);

        GameEvents.RaiseDrawPhaseStarted(drawCount);
        deckManager.DrawCards(drawCount);
        TransitionTo(CombatState.Placement);
    }

    private void EnterPlacement()
    {
        GameEvents.RaisePlacementPhaseStarted();
    }

    private void EnterDiscard()
    {
        int excess = Mathf.Max(0, deckManager.Hand.Count - deckManager.TargetHandSize);
        GameEvents.RaiseDiscardPhaseStarted(excess);
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

    private IEnumerator EnemyTurnRoutine()
    {
        GameEvents.RaiseEnemyTurnStarted();

        // 적의 턴이 시작되면, 직전 방어 턴에 쌓아둔 방어도를 초기화한다.
        enemy.ResetDefense();

        yield return new WaitForSeconds(enemyAttackDelay);

        if (enemyAttackTurn)
        {
            int damage = Mathf.Max(0, enemyBaseDamage - enemyAttackReductionNextTurn);
            enemyAttackReductionNextTurn = 0;
            player.TakeDamage(damage);
        }
        else
        {
            enemy.AddDefense(enemyDefenseAmount);
        }

        enemyAttackTurn = !enemyAttackTurn;

        if (player.IsDead)
        {
            TransitionTo(CombatState.Lose);
            yield break;
        }

        GameEvents.RaiseEnemyIntentChanged(GetCurrentIntent());

        yield return new WaitForSeconds(postAttackDelay);

        TransitionTo(CombatState.PlayerDraw);
    }

    // ─── Event Handlers ───

    // 카드 배치 즉시(OnResolutionResult) / 아티팩트 발동(OnOverlapEffectTriggered) 양쪽 모두
    // 동일한 방식으로 수치를 반영한다.
    private void HandleResolutionResult(ResolutionResult result) => ApplyResolution(result);
    private void HandleOverlapEffectTriggered(ResolutionResult result) => ApplyResolution(result);

    private void ApplyResolution(ResolutionResult result)
    {
        if (result.damage > 0)  enemy.TakeDamage(result.damage);
        if (result.defense > 0) player.AddDefense(result.defense);
        if (result.heal > 0)    player.Heal(result.heal);
        if (result.draw > 0)    extraDrawNextTurn += result.draw;
        if (result.drawNow > 0) deckManager.DrawCards(result.drawNow);
        if (result.enemyAttackReduction > 0) enemyAttackReductionNextTurn += result.enemyAttackReduction;

        if (enemy.IsDead) TransitionTo(CombatState.Win);
    }

    // 카드 효과는 배치 즉시 결산되므로, 턴 종료 시엔 다음 페이즈(버리기/적 턴)로 바로 분기한다.
    private void HandleTurnEndRequested()
    {
        if (currentState != CombatState.Placement)
        {
            Debug.LogWarning("[CombatManager] 배치 페이즈가 아닌데 턴 종료 요청이 옴 — 무시");
            return;
        }

        bool needsDiscard = deckManager.Hand.Count > deckManager.TargetHandSize;
        TransitionTo(needsDiscard ? CombatState.Discard : CombatState.EnemyTurn);
    }

    private void HandleDiscardConfirmed(IReadOnlyList<CardData> cards)
    {
        if (currentState != CombatState.Discard) return;

        deckManager.DiscardCards(cards);
        TransitionTo(CombatState.EnemyTurn);
    }
}
