using UnityEngine;

/// <summary>
/// Visual hand UI. Spawns a card prefab whenever DeckManager raises OnCardDrawn.
/// </summary>
public class HandView : MonoBehaviour
{
    [Header("Prefab and References")]
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform handContainer;

    /// <summary> 손패 카드들의 부모 Transform. 버리기 UI(DiscardSelectionView)에서 참조한다. </summary>
    public Transform HandContainer => handContainer;

    private void Awake()
    {
        if (handContainer == null)
            handContainer = transform;
    }

    private void OnEnable()
    {
        GameEvents.OnCardDrawn += HandleCardDrawn;
    }

    private void OnDisable()
    {
        GameEvents.OnCardDrawn -= HandleCardDrawn;
    }

    private void HandleCardDrawn(CardData cardData)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("[HandView] CardPrefab is not assigned.");
            return;
        }

        CardView newCard = Instantiate(cardPrefab, handContainer);
        newCard.Setup(cardData);
    }
}
