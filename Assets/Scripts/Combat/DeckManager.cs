using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 덱 관리 시스템 — 셔플, 드로우, 핸드, 무덤(Discard) 관리.
///
/// <para>
/// <b>라이프사이클:</b> 손패는 턴을 넘어 유지된다.
/// <list type="number">
///   <item>Initialize()로 초기 덱 구성</item>
///   <item>매 턴 시작 시 손패가 TargetHandSize보다 적으면 그만큼만 DrawCards()로 보충</item>
///   <item>UseCard()로 배치 즉시 사용한 카드를 무덤으로</item>
///   <item>턴 종료 시 손패가 TargetHandSize를 초과하면 DiscardCards()로 초과분 확정 버리기</item>
///   <item>드로우 파일이 비면 무덤을 자동 셔플하여 덱에 복귀</item>
/// </list>
/// </para>
/// </summary>
public class DeckManager : MonoBehaviour
{
    [Header("초기 덱 구성")]
    [Tooltip("전투 시작 시 덱에 포함될 카드 목록 (중복 가능)")]
    [SerializeField] private List<CardData> starterDeck = new();

    [Header("드로우 설정")]
    [Tooltip("턴 시작 시 손패를 이 장수까지 채운다 (이미 이만큼 있으면 드로우하지 않음)")]
    [SerializeField] private int targetHandSize = 7;

    // ── Runtime State ──
    private readonly List<CardData> drawPile = new();
    private readonly List<CardData> hand = new();
    private readonly List<CardData> discardPile = new();

    // ── Public Read-Only Access ──
    public IReadOnlyList<CardData> Hand => hand;
    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;
    public int TargetHandSize => targetHandSize;

    private void OnEnable()
    {
        GameEvents.OnBlockPlaced += HandleBlockPlaced;
    }

    private void OnDisable()
    {
        GameEvents.OnBlockPlaced -= HandleBlockPlaced;
    }

    private void HandleBlockPlaced(CardData card, int x, int y)
    {
        UseCard(card);
    }

    /// <summary>
    /// 덱을 초기화하고 셔플한다. 전투 시작 시 CombatManager가 호출한다.
    /// </summary>
    public void Initialize()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();

        drawPile.AddRange(starterDeck);
        Shuffle(drawPile);
    }

    /// <summary>
    /// 지정된 수만큼 카드를 드로우하여 핸드에 추가한다.
    /// 드로우 파일이 부족하면 무덤을 셔플하여 보충한다.
    /// </summary>
    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 드로우 파일이 비었으면 무덤 → 드로우 파일로 셔플
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    Debug.LogWarning("[DeckManager] 드로우 파일과 무덤 모두 비어 있음 — 드로우 중단");
                    break;
                }

                RecycleDiscardPile();
            }

            CardData card = drawPile[^1];
            drawPile.RemoveAt(drawPile.Count - 1);
            hand.Add(card);

            GameEvents.RaiseCardDrawn(card);
        }
    }

    /// <summary>
    /// 핸드에서 특정 카드를 사용(소비)하고 무덤으로 보낸다.
    /// </summary>
    public bool UseCard(CardData card)
    {
        if (!hand.Remove(card))
        {
            Debug.LogWarning($"[DeckManager] 핸드에 없는 카드를 사용하려 함: {card.CardName}");
            return false;
        }

        discardPile.Add(card);
        GameEvents.RaiseCardUsed(card);
        return true;
    }

    /// <summary>
    /// 지정된 카드들을 손패에서 제거하고 무덤으로 보낸다.
    /// 손패 초과 시 플레이어가 선택한 카드를 확정 버리기할 때 호출.
    /// </summary>
    public void DiscardCards(IReadOnlyList<CardData> cards)
    {
        foreach (var card in cards)
        {
            if (hand.Remove(card))
                discardPile.Add(card);
        }
    }

    // ─── Private Helpers ───

    /// <summary>
    /// 무덤의 모든 카드를 드로우 파일로 옮기고 셔플한다.
    /// </summary>
    private void RecycleDiscardPile()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
        Debug.Log("[DeckManager] 무덤을 셔플하여 드로우 파일에 보충");
    }

    /// <summary>
    /// Fisher-Yates 셔플 알고리즘.
    /// </summary>
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
