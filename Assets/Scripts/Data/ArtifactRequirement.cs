using System;

/// <summary>
/// 아티팩트 발동에 필요한 색상 하나의 요구치. ArtifactData의 requirements 리스트 원소로 사용된다.
/// </summary>
[Serializable]
public struct ArtifactRequirement
{
    public SymbolType color;
    public int requiredCount;
}
