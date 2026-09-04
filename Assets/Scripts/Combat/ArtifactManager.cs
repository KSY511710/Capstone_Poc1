using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장착된 아티팩트들의 색상 충전 진행도를 관리하고, 조건이 충족되면 효과를 발동시킨다.
///
/// <para>
/// <b>동작 방식:</b> 그리드에서 카드가 겹칠 때마다 GridManager가 발행하는
/// <see cref="GameEvents.OnGridColorOverlapped"/>를 받아 해당 색을 요구하는 아티팩트의
/// 진행도를 누적한다. 한 아티팩트가 요구하는 모든 색상이 각각 임계치 이상이 되면
/// 발동하고, 사용한 만큼만 차감한다(초과분은 다음 충전으로 이월). 진행도는 전투 시작 시
/// 초기화되며, 매 턴 그리드가 비워질 때는 리셋되지 않는다.
/// </para>
/// </summary>
public class ArtifactManager : MonoBehaviour
{
    [Header("보유 아티팩트 (테스트용 고정 리스트)")]
    [SerializeField] private List<ArtifactData> equippedArtifacts = new();

    /// <summary> 장착된 아티팩트 목록 (읽기 전용) — UI 표시용. </summary>
    public IReadOnlyList<ArtifactData> EquippedArtifacts => equippedArtifacts;

    // 아티팩트별 · 색상별 누적 진행도
    private readonly Dictionary<ArtifactData, Dictionary<SymbolType, int>> progress = new();

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnGridColorOverlapped += HandleGridColorOverlapped;
        GameEvents.OnCombatStarted       += HandleCombatStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnGridColorOverlapped -= HandleGridColorOverlapped;
        GameEvents.OnCombatStarted       -= HandleCombatStarted;
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    private void HandleCombatStarted()
    {
        progress.Clear();
        foreach (var artifact in equippedArtifacts)
        {
            if (artifact == null) continue;

            var perColor = new Dictionary<SymbolType, int>();
            foreach (var req in artifact.Requirements)
            {
                perColor[req.color] = 0;
                GameEvents.RaiseArtifactProgressChanged(artifact, req.color, 0, req.requiredCount);
            }

            progress[artifact] = perColor;
        }
    }

    private void HandleGridColorOverlapped(SymbolType color, int count)
    {
        foreach (var artifact in equippedArtifacts)
        {
            if (artifact == null || !progress.TryGetValue(artifact, out var perColor)) continue;
            if (!perColor.ContainsKey(color)) continue;

            perColor[color] += count;
            GameEvents.RaiseArtifactProgressChanged(artifact, color, perColor[color], GetRequiredCount(artifact, color));

            while (IsSatisfied(artifact, perColor))
                Trigger(artifact, perColor);
        }
    }

    private static int GetRequiredCount(ArtifactData artifact, SymbolType color)
    {
        foreach (var req in artifact.Requirements)
            if (req.color == color)
                return req.requiredCount;
        return 0;
    }

    private static bool IsSatisfied(ArtifactData artifact, Dictionary<SymbolType, int> perColor)
    {
        foreach (var req in artifact.Requirements)
        {
            if (perColor[req.color] < req.requiredCount)
                return false;
        }
        return true;
    }

    private void Trigger(ArtifactData artifact, Dictionary<SymbolType, int> perColor)
    {
        var result = new ResolutionResult();
        foreach (var effect in artifact.Effects)
            EffectResolver.Apply(ref result, effect);

        foreach (var req in artifact.Requirements)
        {
            perColor[req.color] -= req.requiredCount;
            GameEvents.RaiseArtifactProgressChanged(artifact, req.color, perColor[req.color], req.requiredCount);
        }

        Debug.Log($"[ArtifactManager] {artifact.ArtifactName} 발동 — 공격 {result.damage}, 방어 {result.defense}, 회복 {result.heal}, 드로우 +{result.draw}, 즉시드로우 +{result.drawNow}");
        GameEvents.RaiseOverlapEffectTriggered(result);
    }
}
