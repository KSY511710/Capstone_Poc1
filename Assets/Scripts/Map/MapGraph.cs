using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 노드의 종류. 새 노드 타입을 추가하려면 여기에 값을 추가하고
/// NodeTypeData 에셋을 하나 만든 뒤 GameManager.EnterNode의 분기만 추가하면 된다
/// </summary>
public enum MapNodeType { Combat, Elite, Boss, Shop, Rest, Event, Treasure }

/// <summary>
/// 맵 위의 노드 하나(전투/상점/이벤트 등 한 칸)를 나타내는 런타임 데이터.
/// MapGenerator가 런 시작 시 생성하며, ScriptableObject가 아닌 순수 클래스라
/// JsonUtility로 그대로 직렬화할 수 있다.
/// </summary>
[Serializable]
public class MapNode
{
    // 노드의 고유 식별자. "r2_c1"처럼 행/컬럼을 조합해 생성.
    // 노드끼리 직접 참조 대신 이 id 문자열로 연결하므로 나중에 JSON으로 저장해도 참조가 깨지지 않는다.
    public string id;

    // 이 노드가 속한 층(floor) 인덱스. 0 = 맵 시작 층.
    public int row;

    // 같은 층 안에서 이 노드의 가로 위치(컬럼). 노드 배치 좌표 계산에 사용.
    public int column;

    // 격자 좌표(row, column)에서 화면 표시 위치를 살짝 흔들기 위한 정규화 오프셋(x, y 각각 -1~1).
    // MapGenerator가 생성 시점에 딱 한 번 랜덤으로 정해서 저장해둔다 — 그래서 맵 화면을 다시 그려도
    // (예: 전투 후 Map 씬 복귀) 노드가 항상 같은 위치에 보인다(재현성). 실제 픽셀 단위로 변환하는 건
    // MapView의 몫이다 — 여기서는 유니티 UI 값(px)에 의존하지 않기 위해 정규화 값만 갖는다.
    public Vector2 jitterOffset;

    // 이 노드의 종류(전투/상점/휴식 등). NodeTypeData 조회 키로도 쓰인다.
    public MapNodeType nodeType;

    // 이 노드가 전투류(Combat/Elite/Boss)일 때, 실제로 어떤 적과 싸울지를 가리키는 데이터.
    // MapGenerator가 노드 생성 시점에 NodeTypeData.encounterTable에서 가중치 랜덤으로 뽑아 미리 박아둔다.
    // 전투류가 아닌 노드는 null로 둔다.
    public EncounterData encounter;

    // 다음 층에서 이 노드와 연결된 노드들의 id 목록(간선의 출발점 = 이 노드).
    // MapNode 객체를 직접 들고 있지 않고 id 문자열만 저장 — 순환 참조 방지 + JSON 직렬화 대비
    public List<string> nextNodeIds = new();

    // 플레이어가 이미 지나간 노드인지 여부. true면 맵 UI에서 회색 처리 등으로 표시.
    public bool visited;

    // 플레이어의 현재 위치에서 지금 선택 가능한 노드인지 여부.
    // 매번 currentNodeId가 바뀔 때마다 갱신해줘야 한다.
    public bool available;
}

/// <summary>
/// 두 노드를 잇는 간선 하나. MapGraph.edges에 담겨 맵 UI가 연결선을 그리는 데 쓰인다.
/// </summary>
[Serializable]
public class MapEdge
{
    // 간선이 시작되는 노드의 id (하위 층 → 상위 층 방향).
    public string fromNodeId;

    // 간선이 도착하는 노드의 id.
    public string toNodeId;
}

/// <summary>
/// 한 런(run)의 맵 전체를 표현하는 그래프. MapGenerator.Generate()의 반환값이며
/// RunState.currentMap에 보관되어 Map 씬과 GameManager가 공유해서 읽는다.
/// </summary>
[Serializable]
public class MapGraph
{
    // 이 맵을 생성할 때 사용한 난수 시드. 같은 seed면 항상 같은 맵이 나오도록 보장(재현성).
    public int seed;

    // 이 맵에 존재하는 모든 노드 목록.
    public List<MapNode> nodes = new();

    // 이 맵에 존재하는 모든 간선 목록.
    public List<MapEdge> edges = new();

    // 플레이어가 현재 위치한 노드의 id. 아직 아무 노드도 선택하지 않았다면 비어있음(맵 시작 상태).
    public string currentNodeId;

    /// <summary>
    /// id로 노드를 찾는다. GameManager.RefreshAvailableNodes 등에서 사용.
    /// </summary>
    /// <param name="id">찾을 노드의 id</param>
    /// <returns>일치하는 노드, 없으면 null</returns>
    public MapNode GetNode(string id) => nodes.Find(n => n.id == id);
}
