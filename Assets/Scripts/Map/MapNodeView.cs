using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 노드 하나를 표시하는 뷰. 클릭 시 GameEvents.OnMapNodeSelected를 발행할 뿐,
/// GameManager나 MapController를 직접 참조하지 않는다 
/// </summary>
public class MapNodeView : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    // 이 뷰가 표현하는 노드 데이터. 클릭 시 이 값을 그대로 이벤트에 실어 보낸다.
    private MapNode node;

    /// <summary>
    /// 이 뷰가 표현할 노드 데이터를 지정한다. MapView가 인스턴스화 직후 호출한다.
    /// </summary>
    /// <param name="node">이 뷰가 표현할 맵 노드</param>
    public void Setup(MapNode node)
    {
        this.node = node;
        button.onClick.AddListener(HandleClick);
        Refresh();
    }

    /// <summary>
    /// node의 현재 상태(available/visited/nodeType)를 반영해 겉모습을 갱신한다.
    /// GameEvents.OnMapUpdated 발행 시 MapView가 모든 노드 뷰에 대해 이 메서드를 다시 호출한다.
    /// </summary>
    public void Refresh()
    {
        button.interactable = node.available;
        if (label != null) label.text = node.nodeType.ToString();
        // node.nodeType에 대응하는 NodeTypeData를 조회해 iconImage.sprite에 대입.
        // node.visited면 회색조 처리 등으로 "이미 지나간 노드" 표시.
    }

    /// <summary>
    /// 버튼 클릭 시 호출. 선택 가능한 노드일 때만 이벤트를 발행하고,
    /// 실제 판단/처리는 GameManager.EnterNode에 맡긴다.
    /// </summary>
    private void HandleClick()
    {
        if (!node.available) return;
        GameEvents.RaiseMapNodeSelected(node);
    }
}
