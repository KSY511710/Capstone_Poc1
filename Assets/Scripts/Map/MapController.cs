using UnityEngine;

/// <summary>
/// Map 씬의 진입점. MapNodeView가 발행하는 노드 선택 이벤트를 구독해서
/// 실제 처리(검증, 방문 처리, 씬 전환 등)는 전부 GameManager에 위임한다.
/// </summary>
public class MapController : MonoBehaviour
{
    // 씬이 활성화되는 동안 노드 선택 이벤트 구독.
    private void OnEnable() => GameEvents.OnMapNodeSelected += HandleNodeSelected;

    // 비활성화 시 구독 해제 — 좀비 리스너 방지.
    private void OnDisable() => GameEvents.OnMapNodeSelected -= HandleNodeSelected;

    /// <summary>
    /// 노드가 선택됐을 때 호출되는 핸들러. GameManager.EnterNode로 그대로 위임한다.
    /// </summary>
    /// <param name="node">플레이어가 클릭한 맵 노드</param>
    private void HandleNodeSelected(MapNode node) => GameManager.Instance.EnterNode(node);

    /// <summary>
    /// 씬 전환 시 좀비 리스너 방지 — CombatBootstrap.OnDestroy와 동일한 관례.
    /// </summary>
    private void OnDestroy() => GameEvents.ClearAll();
}
