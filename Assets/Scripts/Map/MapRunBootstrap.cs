using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Map 씬에 배치해서 런을 실제로 시작시키는 테스트/진입용 부트스트랩.
/// Inspector에 꽂힌 MapGenerationConfig/시작 덱/시작 아티팩트로 GameManager.StartNewRun을 호출한다.
/// GameManager.Awake()는 싱글톤 등록만 하고 런을 자동으로 시작하지 않으므로,
/// 이 컴포넌트가 없으면 Run이 계속 null인 채로 남아 MapView가 죽는다.
/// </summary>
public class MapRunBootstrap : MonoBehaviour
{
    [Header("맵 생성 설정")]
    [Tooltip("이번 런에 사용할 맵 생성 규칙 (층 수/노드 타입 가중치 등)")]
    [SerializeField] private MapGenerationConfig config;

    [Header("시작 자원")]
    [Tooltip("런 시작 시 플레이어가 갖고 시작할 카드 목록")]
    [SerializeField] private List<CardData> startingDeck = new();
    [Tooltip("런 시작 시 플레이어가 갖고 시작할 아티팩트 목록")]
    [SerializeField] private List<ArtifactData> startingArtifacts = new();

    /// <summary>
    /// 씬 시작 시, 아직 진행 중인 런이 없을 때만(=Map 씬에 처음 진입했을 때만) 새 런을 시작한다.
    /// 전투 후 Map 씬으로 복귀하는 경우에는 GameManager.Run이 이미 존재하므로 재생성하지 않는다.
    /// </summary>
    private void Start()
    {
        if (GameManager.Instance.Run == null)
        {
            GameManager.Instance.StartNewRun(config, startingDeck, startingArtifacts);
        }
    }
}
