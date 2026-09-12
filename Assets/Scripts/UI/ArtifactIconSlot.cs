using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 아티팩트 아이콘 하나. 우하단에 요구 색상별 충전 카운트(예: 3/3 1/2)를 표시하고,
/// 마우스 호버 시 <see cref="ArtifactIconBar"/>에 툴팁 표시를 요청한다.
/// </summary>
public class ArtifactIconSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ArtifactIconBar owner;
    private TextMeshProUGUI countText;

    // 요구 색상별 현재 진행도 (Requirements 순서를 유지하기 위해 색상 목록도 따로 보관)
    private readonly List<SymbolType> requiredColors = new();
    private readonly Dictionary<SymbolType, int> currentCounts = new();

    public ArtifactData Artifact { get; private set; }

    public void Setup(ArtifactIconBar owner, ArtifactData artifact, TextMeshProUGUI countText)
    {
        this.owner = owner;
        this.countText = countText;
        Artifact = artifact;

        requiredColors.Clear();
        currentCounts.Clear();
        foreach (var req in artifact.Requirements)
        {
            if (!currentCounts.ContainsKey(req.color))
            {
                requiredColors.Add(req.color);
                currentCounts[req.color] = 0;
            }
        }

        RefreshText();
    }

    /// <summary> 특정 요구 색상의 진행도를 갱신한다. </summary>
    public void SetCount(SymbolType color, int current, int required)
    {
        currentCounts[color] = current;
        RefreshText();
    }

    private void RefreshText()
    {
        if (countText == null) return;

        var builder = new StringBuilder();
        for (int i = 0; i < requiredColors.Count; i++)
        {
            SymbolType color = requiredColors[i];
            int required = GetRequiredCount(color);
            int current = currentCounts.TryGetValue(color, out int c) ? c : 0;

            string hex = ColorUtility.ToHtmlStringRGB(SymbolVisuals.GetColor(color));
            builder.Append($"<color=#{hex}>{current}/{required}</color>");

            if (i < requiredColors.Count - 1)
                builder.Append(' ');
        }

        countText.SetText(builder.ToString());
    }

    private int GetRequiredCount(SymbolType color)
    {
        foreach (var req in Artifact.Requirements)
            if (req.color == color)
                return req.requiredCount;
        return 0;
    }

    public void OnPointerEnter(PointerEventData eventData) => owner.ShowTooltip(Artifact);
    public void OnPointerExit(PointerEventData eventData) => owner.HideTooltip();
}
