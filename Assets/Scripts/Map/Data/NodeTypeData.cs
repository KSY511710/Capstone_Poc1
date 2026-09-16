using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 노드 타입 하나(전투/상점/휴식 등)의 기획 데이터를 정의하는 ScriptableObject.
/// 에디터에서 값을 채운 에셋을 Assets/Data/Map/NodeTypes/ 아래에 타입별로 하나씩 만들어 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "NewNodeTypeData", menuName = "DeckBuilder/Map/Node Type Data")]
public class NodeTypeData : ScriptableObject
{
    // 이 데이터가 어떤 MapNodeType에 대응하는지.
    [SerializeField] private MapNodeType nodeType;

    // 맵 UI 등에 표시할 노드 이름 (예: "일반 전투", "휴식처").
    [SerializeField] private string displayName = "New Node Type";

    // 맵 위에서 이 노드 타입을 나타낼 아이콘 스프라이트.
    [SerializeField] private Sprite icon;

    // 층(row)별로 이 노드 타입이 등장할 가중치. MapGenerator가 타입 배정 시 참조한다.
    [SerializeField] private List<RowWeight> weightsByRow = new();

    // 이 노드 타입이 Combat/Elite/Boss일 때 사용할 조우(적 구성) 후보 테이블.
    [SerializeField] private List<EncounterEntry> encounterTable = new();

    // nodeType의 읽기 전용 접근자.
    public MapNodeType NodeType => nodeType;

    // displayName의 읽기 전용 접근자.
    public string DisplayName => displayName;

    // icon의 읽기 전용 접근자.
    public Sprite Icon => icon;

    // weightsByRow의 읽기 전용 접근자. 외부에서 리스트를 수정하지 못하도록 IReadOnlyList로 노출.
    public IReadOnlyList<RowWeight> WeightsByRow => weightsByRow;

    // encounterTable의 읽기 전용 접근자.
    public IReadOnlyList<EncounterEntry> EncounterTable => encounterTable;
}

/// <summary>
/// weightsByRow 한 줄 — minRow층 이상부터 이 가중치를 적용한다는 뜻.
/// 예: (minRow=0, weight=1), (minRow=5, weight=3)이면 5층부터 이 노드 타입이 3배 더 잘 나옴.
/// </summary>
[Serializable]
public struct RowWeight
{
    public int minRow;
    public float weight;
}

/// <summary>
/// 특정 EncounterData가 뽑힐 가중치.
/// weight가 클수록 MapGenerator가 이 조우를 더 자주 선택한다.
/// </summary>
[Serializable]
public struct EncounterEntry
{
    public EncounterData encounter;
    public float weight;
}
