using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 손패 초과 시 버릴 카드를 선택하는 화면.
///
/// <para>
/// <b>동작 방식:</b> 오버레이/선택 줄/확인 버튼은 씬에서 직접 만들어 인스펙터로 연결한다 —
/// 이 스크립트는 그것들의 활성화 여부만 토글한다. CombatState.Discard 진입 시
/// (<see cref="GameEvents.OnDiscardPhaseStarted"/>) 오버레이를 켜고 손패 카드에 클릭 리스너를
/// 붙인다. 카드를 클릭하면 선택 줄(selectionRow)로 옮겨지고(다시 클릭하면 손패로 복귀),
/// 필요한 장수를 정확히 채우면 확인 버튼이 활성화된다. 확인을 누르면
/// <see cref="GameEvents.OnDiscardConfirmed"/>를 발행하고 선택된 카드의 시각 오브젝트를
/// 파괴한다. 실제 덱 데이터 처리는 CombatManager/DeckManager가 담당.
/// </para>
/// </summary>
public class DiscardSelectionView : MonoBehaviour
{
    [Header("Scene References (직접 만든 UI를 연결)")]
    [Tooltip("버리기 페이즈 동안 켜고 끌 오버레이 루트. 평소엔 비활성화 상태로 둔다.")]
    [SerializeField] private GameObject overlayRoot;
    [Tooltip("선택된 카드가 옮겨질 부모 (가로 배치용 HorizontalLayoutGroup 등)")]
    [SerializeField] private Transform selectionRow;
    [SerializeField] private Button confirmButton;
    [Tooltip("선택 현황 안내 텍스트. 없어도 동작에는 지장 없음.")]
    [SerializeField] private TextMeshProUGUI promptText;

    private HandView handView;
    private int requiredCount;
    private readonly List<CardView> selected = new();
    private readonly List<DiscardCardClickRelay> relays = new();

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnDiscardPhaseStarted += HandleDiscardPhaseStarted;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(HandleConfirmClicked);
    }

    private void OnDisable()
    {
        GameEvents.OnDiscardPhaseStarted -= HandleDiscardPhaseStarted;

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);

        Teardown();
    }

    // ═══════════════════════════════════════════
    //  Public API (DiscardCardClickRelay에서 호출)
    // ═══════════════════════════════════════════

    public void ToggleSelect(CardView cardView)
    {
        if (cardView == null) return;

        if (selected.Contains(cardView))
        {
            Deselect(cardView);
        }
        else if (selected.Count < requiredCount)
        {
            Select(cardView);
        }
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    private void HandleDiscardPhaseStarted(int count)
    {
        requiredCount = count;
        selected.Clear();

        handView = FindAnyObjectByType<HandView>();

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        AttachRelaysToHand();
        RefreshPrompt();
    }

    private void AttachRelaysToHand()
    {
        if (handView == null || handView.HandContainer == null) return;

        foreach (Transform child in handView.HandContainer)
        {
            var cardView = child.GetComponent<CardView>();
            if (cardView == null) continue;

            var relay = child.gameObject.AddComponent<DiscardCardClickRelay>();
            relay.Init(this, cardView);
            relays.Add(relay);
        }
    }

    private void Select(CardView cardView)
    {
        selected.Add(cardView);
        cardView.transform.SetParent(selectionRow, false);
        cardView.transform.localScale = Vector3.one;
        cardView.transform.localRotation = Quaternion.identity;

        // SetParent(false)는 anchoredPosition을 그대로 유지한다 — 손패의 부채꼴 배치에서
        // 쓰던 큰 x 오프셋이 selectionRow 기준으로 재해석되면 화면 밖으로 나가버리므로 0으로 리셋.
        // 실제 정렬은 selectionRow의 HorizontalLayoutGroup이 담당한다.
        if (cardView.transform is RectTransform rect)
            rect.anchoredPosition = Vector2.zero;

        RefreshPrompt();
    }

    private void Deselect(CardView cardView)
    {
        selected.Remove(cardView);

        if (handView != null && handView.HandContainer != null)
            cardView.transform.SetParent(handView.HandContainer, false);

        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (promptText != null)
            promptText.text = $"버릴 카드를 선택하십시오 ({selected.Count}/{requiredCount})";

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(selected.Count == requiredCount);
    }

    private void HandleConfirmClicked()
    {
        var discardedData = new List<CardData>(selected.Count);
        foreach (var cardView in selected)
        {
            if (cardView != null && cardView.CurrentCardData != null)
                discardedData.Add(cardView.CurrentCardData);
        }

        foreach (var cardView in selected)
        {
            if (cardView != null)
                Destroy(cardView.gameObject);
        }

        GameEvents.RaiseDiscardConfirmed(discardedData);
        Teardown();
    }

    private void Teardown()
    {
        foreach (var relay in relays)
        {
            if (relay != null)
                Destroy(relay);
        }
        relays.Clear();
        selected.Clear();

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }
}
