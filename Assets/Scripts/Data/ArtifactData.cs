using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아티팩트 하나의 정적 데이터를 정의하는 ScriptableObject.
/// 그리드에서 특정 색상이 요구치만큼 겹치면 효과가 발동한다(ArtifactManager 참고).
/// </summary>
[CreateAssetMenu(fileName = "NewArtifactData", menuName = "DeckBuilder/Artifact Data")]
public class ArtifactData : ScriptableObject
{
    [Header("아티팩트 기본 정보")]
    [Tooltip("아티팩트 이름 (UI 표시용)")]
    [SerializeField] private string artifactName = "New Artifact";

    [Tooltip("아티팩트 설명 (UI 표시용)")]
    [TextArea(2, 4)]
    [SerializeField] private string description = "";

    [Tooltip("아이콘 (UI 표시용). 비워두면 iconColor로 칠해진 사각형이 대신 표시된다.")]
    [SerializeField] private Sprite icon;

    [Tooltip("아이콘 배경/틴트 색상. 요구 색상과 무관하게 직접 지정한다.")]
    [SerializeField] private Color iconColor = Color.white;

    [Header("발동 조건")]
    [Tooltip("발동에 필요한 색상별 요구 카운트. 모두 충족해야 발동한다.")]
    [SerializeField] private List<ArtifactRequirement> requirements = new();

    [Header("발동 효과")]
    [Tooltip("발동 시 적용되는 효과 목록.")]
    [SerializeField] private List<CardEffect> effects = new();

    // ── Public Properties ──
    public string ArtifactName => artifactName;
    public string Description => description;
    public Sprite Icon => icon;
    public Color IconColor => iconColor;
    public IReadOnlyList<ArtifactRequirement> Requirements => requirements;
    public IReadOnlyList<CardEffect> Effects => effects;
}
