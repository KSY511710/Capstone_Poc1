using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 절차적 생성에 필요한 모든 설정값을 모아두는 ScriptableObject.
/// MapGenerator.Generate(config, seed)의 config 인자로 사용된다.
/// </summary>
[CreateAssetMenu(fileName = "NewMapGenerationConfig", menuName = "DeckBuilder/Map/Map Generation Config")]
public class MapGenerationConfig : ScriptableObject
{
    [Header("맵 크기")]
    [Tooltip("보스 층을 제외한 일반 층 수")]
    [SerializeField] private int rowCount = 15;
    [Tooltip("각 층에서 노드가 놓일 수 있는 컬럼(가로 칸) 수")]
    [SerializeField] private int columnsPerRow = 6;
    [Tooltip("0층에서 시작해 보스까지 뻗어나가는 경로(길)의 개수 — 많을수록 노드/간선이 촘촘해짐")]
    [SerializeField] private int pathCount = 6;

    [Header("특수 층 규칙")]
    [Tooltip("보스 층 바로 아래 몇 개 층을 강제로 Rest 노드로 만들지")]
    [SerializeField] private int restRowsBeforeBoss = 1;

    [Header("노드 타입 데이터")]
    [Tooltip("이 맵에서 사용할 노드 타입별 기획 데이터 (Boss/Rest 포함 전체 등록)")]
    [SerializeField] private List<NodeTypeData> nodeTypes = new();

    public int RowCount => rowCount;
    public int ColumnsPerRow => columnsPerRow;
    public int PathCount => pathCount;
    public int RestRowsBeforeBoss => restRowsBeforeBoss;
    public IReadOnlyList<NodeTypeData> NodeTypes => nodeTypes;

    /// <summary>
    /// 등록된 노드 타입 데이터 중 지정한 MapNodeType에 해당하는 것을 찾아 반환한다.
    /// 없으면 null을 반환하므로 호출부에서 null 체크가 필요하다.
    /// </summary>
    /// <param name="type">찾을 노드 타입</param>
    public NodeTypeData GetNodeTypeData(MapNodeType type) => nodeTypes.Find(n => n.NodeType == type);
}
