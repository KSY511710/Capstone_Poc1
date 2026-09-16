using UnityEngine;

/// <summary>
/// Combat 씬에 CombatBootstrap과 나란히 배치하는 컴포넌트.
/// GameManager가 들고 있는 RunState의 덱/아티팩트/조우 데이터를 이번 전투의
/// 각 매니저에 주입한다. 기존 CombatBootstrap.cs는 건드리지 않는다 —
/// GameManager 없이(Map 씬을 거치지 않고) 이 전투 씬을 바로 Play해서 테스트하는
/// 기존 작업 흐름을 깨지 않기 위함.
/// </summary>
public class RunCombatBootstrap : MonoBehaviour
{
    [Header("참조 — 같은 씬의 매니저들")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private ArtifactManager artifactManager;
    [SerializeField] private CombatManager combatManager;

    /// <summary>
    /// CombatBootstrap.Start()보다 먼저 실행되어야 하므로 Awake에서 처리한다
    /// (유니티는 씬의 모든 Awake를 모든 Start보다 먼저 실행하므로, 같은 씬 안에서는
    /// 실행 순서(DefaultExecutionOrder) 없이도 항상 CombatBootstrap.Start()보다 먼저 끝난다).
    /// GameManager.Instance가 없다면(=Map 씬을 거치지 않고 이 씬을 바로 Play한 경우)
    /// 아무 것도 하지 않고 CombatBootstrap이 기존 Inspector 고정값으로 동작하도록 둔다.
    /// </summary>
    private void Awake()
    {
        if (GameManager.Instance == null) return;

        RunState run = GameManager.Instance.Run;
        deckManager.SetStarterDeck(run.deck);
        artifactManager.SetEquippedArtifacts(run.artifacts);
        combatManager.SetEncounter(run.pendingEncounter);
    }
}
