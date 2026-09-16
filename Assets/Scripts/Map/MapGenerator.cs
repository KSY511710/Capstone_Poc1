using System.Collections.Generic;
using System.Linq;
using UnityEngine;  // Vector2(jitterOffset 계산)

/// <summary>
/// MapGenerationConfig와 seed를 받아 MapGraph를 절차적으로 만들어내는 정적 생성기.
/// MonoBehaviour가 아닌 순수 static 클래스 — 씬/오브젝트에 의존하지 않고
/// 어디서든(에디터 툴, 테스트 코드 등) 호출 가능하게 하기 위함.
/// </summary>
public static class MapGenerator
{
    /// <summary>
    /// 맵 그래프 하나를 생성한다. 같은 config+seed 조합이면 항상 같은 결과가 나온다(재현성).
    /// </summary>
    /// <param name="config">층 수/컬럼 수/경로 수/노드 타입 가중치 등 생성 설정</param>
    /// <param name="seed">난수 시드. UnityEngine.Random이 아닌 별도 System.Random을 써서
    /// 전투 등 다른 곳의 UnityEngine.Random 소비와 간섭하지 않게 한다.</param>
    /// <returns>생성이 끝난 MapGraph. currentNodeId는 비어있는 상태(=맵 시작 전)로 반환된다.</returns>
    public static MapGraph Generate(MapGenerationConfig config, int seed)
    {
        var random = new System.Random(seed);
        var graph = new MapGraph { seed = seed };

        // 노드를 (row, col) 좌표로 찾기 위한 보조 인덱스. 같은 좌표에 경로가 여러 번 지나가도
        // 노드를 중복 생성하지 않고 재사용하기 위해 필요하다.
        var nodesByCoord = new Dictionary<(int row, int col), MapNode>();

        // ── 1단계: 경로 먼저 생성 ──
        // pathCount개의 경로를 0층의 임의 컬럼에서 시작해서, 매 층마다 컬럼을 -1/0/+1 중
        // 하나만 이동시키며 끝층까지 이어간다. 이렇게 하면 간선이 인접 컬럼끼리만 이어져서
        // 화면에 그렸을 때 선끼리 심하게 교차하지 않는다.
        for (int p = 0; p < config.PathCount; p++)
        {
            int col = random.Next(0, config.ColumnsPerRow);
            MapNode previous = GetOrCreateNode(graph, nodesByCoord, 0, col, random);

            for (int row = 1; row < config.RowCount; row++)
            {
                int delta = random.Next(-1, 2); // -1, 0, 1 중 하나
                col = Clamp(col + delta, 0, config.ColumnsPerRow - 1);
                MapNode current = GetOrCreateNode(graph, nodesByCoord, row, col, random);

                // 같은 간선이 여러 경로에서 중복 생성될 수 있으므로 존재 여부를 먼저 확인한다.
                if (!previous.nextNodeIds.Contains(current.id))
                {
                    previous.nextNodeIds.Add(current.id);
                    graph.edges.Add(new MapEdge { fromNodeId = previous.id, toNodeId = current.id });
                }

                previous = current;
            }
        }

        // ── 2단계: 보스 노드 생성 후 마지막 층 전체를 보스로 합류 ──
        // 보스는 단 하나뿐이라 레인 중앙에 고정 배치하는 게 자연스러우므로 지터는 주지 않는다(기본값 Vector2.zero).
        var bossNode = new MapNode { id = "boss", row = config.RowCount, column = 0, nodeType = MapNodeType.Boss };
        graph.nodes.Add(bossNode);

        foreach (MapNode lastRowNode in graph.nodes.Where(n => n.row == config.RowCount - 1).ToList())
        {
            lastRowNode.nextNodeIds.Add(bossNode.id);
            graph.edges.Add(new MapEdge { fromNodeId = lastRowNode.id, toNodeId = bossNode.id });
        }

        // ── 3단계: 노드 타입 배정 ──
        // 0층은 항상 일반 전투로 고정, 보스 직전 restRowsBeforeBoss개 층은 강제 Rest,
        // 그 사이 층들은 NodeTypeData의 층별 가중치로 랜덤 배정한다.
        int restRowStart = config.RowCount - config.RestRowsBeforeBoss;
        foreach (MapNode node in graph.nodes)
        {
            if (node.nodeType == MapNodeType.Boss) continue; // 이미 배정됨

            if (node.row == 0)
                node.nodeType = MapNodeType.Combat;
            else if (node.row >= restRowStart)
                node.nodeType = MapNodeType.Rest;
            else
                node.nodeType = PickWeightedNodeType(config, node.row, random);
        }

        // ── 4단계: 전투류 노드에 조우 데이터 미리 뽑아 박아두기 ──
        foreach (MapNode node in graph.nodes)
        {
            if (node.nodeType == MapNodeType.Combat || node.nodeType == MapNodeType.Elite || node.nodeType == MapNodeType.Boss)
            {
                NodeTypeData typeData = config.GetNodeTypeData(node.nodeType);
                node.encounter = PickWeightedEncounter(typeData, random);
            }
        }

        // ── 5단계: 고아 노드 검증 ──
        // 0층이 아닌데 들어오는 간선이 하나도 없는 노드가 있으면, 이전 층의 임의 노드에서
        // 강제로 연결해준다. (경로 이동 폭이 제한적이라 이론상 거의 발생하지 않지만 안전장치로 둔다.)
        RepairOrphanNodes(graph, random);

        return graph;
    }

    /// <summary>
    /// (row, col) 좌표에 해당하는 노드를 찾아 반환하거나, 없으면 새로 만들어 그래프에 추가한다.
    /// 새로 만들 때 화면 표시용 지터 오프셋도 함께 뽑아 저장해둔다 — 격자 좌표(row, column) 위에
    /// 노드를 딱딱하게 정렬시키지 않고, 나중에 MapView가 이 값을 픽셀 단위로 확대해서 살짝 흔들리게 그린다.
    /// </summary>
    private static MapNode GetOrCreateNode(MapGraph graph, Dictionary<(int, int), MapNode> nodesByCoord, int row, int col, System.Random random)
    {
        var key = (row, col);
        if (nodesByCoord.TryGetValue(key, out MapNode existing))
            return existing;

        // x, y 각각 -1 ~ 1 범위의 정규화된 오프셋. 실제 px 값은 MapView.jitterRange를 곱해서 결정된다.
        var jitter = new Vector2(
            (float)(random.NextDouble() * 2.0 - 1.0),
            (float)(random.NextDouble() * 2.0 - 1.0));

        var node = new MapNode { id = $"r{row}_c{col}", row = row, column = col, jitterOffset = jitter };
        nodesByCoord[key] = node;
        graph.nodes.Add(node);
        return node;
    }

    /// <summary>
    /// value를 [min, max] 범위로 잘라낸다. UnityEngine 의존 없이 순수 C#으로 동작하게 하기 위한 헬퍼.
    /// </summary>
    private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

    /// <summary>
    /// 특정 층(row)에서 등장 가능한 노드 타입(Boss 제외) 중 하나를 가중치 랜덤으로 뽑는다.
    /// Rest도 여기서 후보에 포함될 수 있다 — 보스 직전 강제 구간과는 별개로,
    /// 중간 층에서도 Weights By Row에 값이 채워져 있으면 랜덤하게 섞여 나온다.
    /// </summary>
    /// <param name="config">전체 노드 타입 목록을 들고 있는 생성 설정</param>
    /// <param name="row">노드 타입을 배정할 층</param>
    /// <param name="random">시드로 초기화된 난수 생성기</param>
    private static MapNodeType PickWeightedNodeType(MapGenerationConfig config, int row, System.Random random)
    {
        var candidates = new List<(MapNodeType type, float weight)>();
        foreach (NodeTypeData typeData in config.NodeTypes)
        {
            if (typeData.NodeType == MapNodeType.Boss) continue;

            float weight = 0f;
            foreach (RowWeight rw in typeData.WeightsByRow)
            {
                if (row >= rw.minRow) weight = rw.weight; // minRow가 가장 큰(=현재 층 이하 중 가장 가까운) 값을 채택
            }
            if (weight > 0f) candidates.Add((typeData.NodeType, weight));
        }

        if (candidates.Count == 0) return MapNodeType.Combat; // 안전 기본값

        float total = candidates.Sum(c => c.weight);
        float roll = (float)random.NextDouble() * total;
        float cumulative = 0f;
        foreach (var candidate in candidates)
        {
            cumulative += candidate.weight;
            if (roll <= cumulative) return candidate.type;
        }
        return candidates[candidates.Count - 1].type;
    }

    /// <summary>
    /// 특정 노드 타입의 encounterTable에서 가중치 랜덤으로 EncounterData 하나를 뽑는다.
    /// </summary>
    /// <param name="typeData">뽑을 대상 노드 타입의 기획 데이터 (없으면 null 반환)</param>
    /// <param name="random">시드로 초기화된 난수 생성기</param>
    private static EncounterData PickWeightedEncounter(NodeTypeData typeData, System.Random random)
    {
        if (typeData == null || typeData.EncounterTable.Count == 0) return null;

        float total = typeData.EncounterTable.Sum(e => e.weight);
        float roll = (float)random.NextDouble() * total;
        float cumulative = 0f;
        foreach (EncounterEntry entry in typeData.EncounterTable)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry.encounter;
        }
        return typeData.EncounterTable[typeData.EncounterTable.Count - 1].encounter;
    }

    /// <summary>
    /// 0층이 아닌데 들어오는 간선이 없는 노드(고아)를 찾아 이전 층의 임의 노드와 강제로 연결한다.
    /// </summary>
    private static void RepairOrphanNodes(MapGraph graph, System.Random random)
    {
        var hasIncoming = new HashSet<string>(graph.edges.Select(e => e.toNodeId));

        foreach (MapNode node in graph.nodes)
        {
            if (node.row == 0 || hasIncoming.Contains(node.id)) continue;

            List<MapNode> previousRow = graph.nodes.Where(n => n.row == node.row - 1).ToList();
            if (previousRow.Count == 0) continue;

            MapNode from = previousRow[random.Next(previousRow.Count)];
            from.nextNodeIds.Add(node.id);
            graph.edges.Add(new MapEdge { fromNodeId = from.id, toNodeId = node.id });
        }
    }
}
