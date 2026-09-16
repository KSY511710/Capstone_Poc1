using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 손패 카드 클릭을 DiscardSelectionView로 전달하는 임시 컴포넌트.
/// 버리기 페이즈 동안에만 DiscardSelectionView가 동적으로 카드에 부착/제거한다.
/// CardView는 드래그 배치만 담당하므로, 클릭 선택 로직을 분리해 서로 독립적으로 유지한다.
/// </summary>
public class DiscardCardClickRelay : MonoBehaviour, IPointerClickHandler
{
    private DiscardSelectionView owner;
    private CardView cardView;

    public void Init(DiscardSelectionView owner, CardView cardView)
    {
        this.owner = owner;
        this.cardView = cardView;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner.ToggleSelect(cardView);
    }
}
