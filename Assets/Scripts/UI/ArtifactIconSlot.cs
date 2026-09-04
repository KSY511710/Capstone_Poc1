using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 아티팩트 아이콘 하나. 우하단에 현재 충전 카운트(예: 0/3)를 표시하고,
/// 마우스 호버 시 <see cref="ArtifactIconBar"/>에 툴팁 표시를 요청한다.
/// </summary>
public class ArtifactIconSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ArtifactIconBar owner;
    private TextMeshProUGUI countText;

    public ArtifactData Artifact { get; private set; }

    public void Setup(ArtifactIconBar owner, ArtifactData artifact, TextMeshProUGUI countText)
    {
        this.owner = owner;
        this.countText = countText;
        Artifact = artifact;

        int required = artifact.Requirements.Count > 0 ? artifact.Requirements[0].requiredCount : 0;
        SetCount(0, required);
    }

    public void SetCount(int current, int required)
    {
        if (countText != null)
            countText.SetText($"{current}/{required}");
    }

    public void OnPointerEnter(PointerEventData eventData) => owner.ShowTooltip(Artifact);
    public void OnPointerExit(PointerEventData eventData) => owner.HideTooltip();
}
