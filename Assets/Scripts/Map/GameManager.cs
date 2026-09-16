
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]  


public class GameManager : MonoBehaviour
{
    [Tooltip("맵 화면 씬 이름")]
    [SerializeField] private string mapSceneName = "KSW_Map";
    [Tooltip("전투 씬 이름 — 현재는 표준 전투 씬 하나만 사용한다고 가정")]
    [SerializeField] private string combatSceneName = "KSW_Stack";
    [Tooltip("런 시작 시 플레이어 최대 체력")]
    [SerializeField] private int startingPlayerMaxHp = 50;
    public static GameManager Instance { get; private set; }
    
    // 현재 진행 중인 런의 상태. 외부에서는 읽기만 가능하고,
    // 수정은 GameManager의 메서드(EnterNode 등)를 통해서만 이뤄진다.
    public RunState Run { get; private set; }
    
    // EnterNode에서 전투 씬으로 넘어갈 때, 어떤 타입의 노드로 들어간 건지 잠깐 기억해두는 값
    private MapNodeType pendingNodeType;
    
    /// <summary>
    /// 씬 로드 시 자동 호출. 이미 인스턴스가 있으면 중복 생성을 막고 자신(컴포넌트)만 파괴하며,
    /// 처음 생성되는 경우에만 싱글톤으로 등록하고 씬 전환에도 파괴되지 않게 한다.
    /// 오브젝트 전체(Destroy(gameObject))를 지우면 같은 오브젝트에 같이 붙어있는 MapController 등
    /// 다른 컴포넌트까지 같이 파괴되어, 맵 씬이 재로드될 때마다 노드 클릭이 씹히는 문제가 있었다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); //파괴하지않는다
    }
    
    /// <summary>
    /// 새 런을 시작한다. config를 바탕으로 맵을 절차적으로 생성하고,
    /// 시작 덱/아티팩트로 RunState를 초기화한 뒤 Map 씬을 로드한다.
    /// </summary>
    /// <param name="config">층 수, 컬럼 수, 노드 타입 가중치 등 맵 생성 설정</param>
    /// <param name="startDeck">런 시작 시 플레이어 덱</param>
    /// <param name="startArtifacts">런 시작 시 플레이어 아티팩트</param>
    public void StartNewRun(MapGenerationConfig config, List<CardData> startDeck, List<ArtifactData> startArtifacts)
    {
        // 매 런마다 다른 맵이 나오도록 현재 시각 기반으로 시드를 뽑는다
        int seed = Environment.TickCount;
        MapGraph generatedMap = MapGenerator.Generate(config, seed);

        Run = new RunState
        {
            currentMap = generatedMap,
            floorNumber = 0,
            currency = 0,
            playerMaxHp = startingPlayerMaxHp,
            playerCurrentHp = startingPlayerMaxHp,
            deck = new List<CardData>(startDeck),
            artifacts = new List<ArtifactData>(startArtifacts)
        };
        // 맵 생성 직후 0층 노드들만 선택 가능한 상태로 표시
        RefreshAvailableNodes();

        // MapRunBootstrap은 Map 씬 안에서 호출되므로, 이미 Map 씬에 있는 상태라면
        // 다시 로드할 필요가 없다(자기 자신을 재로드하면 방금 만든 MapView/버튼들이
        // 전부 파괴됐다가 새로 생성되면서 불필요한 리셋이 발생한다).
        if (SceneManager.GetActiveScene().name != mapSceneName)
        {
            SceneManager.LoadScene(mapSceneName);
        }
    }
    /// <summary>
    /// 플레이어가 맵에서 노드를 선택했을 때 호출
    /// 노드가 선택 가능한 상태인지 검증하고, 방문 처리 후 노드 타입에 따라
    /// 전투 씬 로드 등 알맞은 후속 동작으로 분기
    /// </summary>
    /// <param name="node">플레이어가 클릭해 선택한 맵 노드</param>
    public void EnterNode(MapNode node)
    {
        if (!node.available)
        {
            Debug.LogWarning($"[GameManager] 아직 선택할 수 없는 노드입니다: {node.id}");
            return;
        }
        node.visited = true;
        Run.currentMap.currentNodeId = node.id;
        Run.floorNumber = node.row;
        pendingNodeType = node.nodeType;

        switch (node.nodeType)
        {
            // 전투류 노드 — 기존 Combat 씬으로 진입
            // Elite/Boss도 같은 씬을 재사용하고 어떤 조우를 쓸지는 NodeTypeData.encounterTable로 구분
            case MapNodeType.Combat:
            case MapNodeType.Elite:
            case MapNodeType.Boss:
                // 이 노드에 생성 시점부터 박혀있던 조우 데이터를 RunState로 옮겨서
                // Combat 씬이 로드된 뒤 어떤 적을 스폰할지 읽을 수 있게 한다
                //흠 .. 이거 고민좀 
                Run.pendingEncounter = node.encounter;
                GameEvents.ClearAll();  // 씬 전환 전 초기화
                SceneManager.LoadScene(combatSceneName);
                break;

            // Shop/Rest/Event/Treasure는 아직 별도 시스템이 없으므로 우선 경고만 남기고
            // 맵에 그대로 머무른다
            case MapNodeType.Shop:
            case MapNodeType.Rest:
            case MapNodeType.Event:
            case MapNodeType.Treasure:
                Debug.Log($"[GameManager] '{node.nodeType}' 노드 입장 - id: {node.id} (아직 미구현이라 로그만 남김)");
                RefreshAvailableNodes();
                break;
        }
    }
    
    /// <summary>
    /// 노드(전투/상점/휴식/이벤트 등) 진행을 성공적으로 마치고 맵으로 돌아올 때 호출된다.
    /// 보상 지급·게임오버 판정처럼 각 씬 내부에서 끝나는 처리는 여기서 다루지 않는다 —
    /// 전투의 경우 패배 시에는 애초에 이 메서드를 부르지 않고 해당 씬에서 게임오버 흐름으로 빠지는 게 원칙이다
    /// (CombatResultRelay 참고).
    /// </summary>
    public void ReturnToMap()
    {
        RefreshAvailableNodes();
        GameEvents.ClearAll();
        SceneManager.LoadScene(mapSceneName);
    }
    
    //-------------------------------- 프라이빗 --------------------------------------------------------------------
    // 현재 위치(currentNodeId)를 기준으로 다음에 선택 가능한 노드들의 available 플래그를 갱신한다.
    // 맵을 처음 생성했을 때(currentNodeId가 비어있을 때)는 0층 노드 전체를 선택 가능하게 만든다.
    private void RefreshAvailableNodes()
    {
        MapGraph map = Run.currentMap;
        foreach (MapNode node in map.nodes)
        {
            node.available = false;
        }
        if (string.IsNullOrEmpty(map.currentNodeId))
        {
            foreach (MapNode node in map.nodes)
            {
                if (node.row == 0) node.available = true;
            }
            GameEvents.RaiseMapUpdated();
            return;
        }
        MapNode current = map.GetNode(map.currentNodeId);
        foreach (string nextId in current.nextNodeIds)
        {
            MapNode next = map.GetNode(nextId);
            if (next != null) next.available = true;
        }
        GameEvents.RaiseMapUpdated();
    }
}
