using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장착된 아티팩트를 아이콘으로 가로 나열한다.
/// 각 아이콘 우하단에는 현재 충전 카운트(예: 0/3)가 표시되고, 마우스를 올리면
/// 아이콘 바로 아래에 아티팩트 이름/설명/요구 조건을 보여주는 툴팁이 뜬다.
///
/// <para>
/// <b>씬 설정:</b> 빈 UI GameObject에 이 컴포넌트를 붙이고 RectTransform을
/// 화면 오른쪽 위로 앵커(anchorMin/Max = (1,1))하면 된다. 아이콘 슬롯과 툴팁은
/// 전부 런타임에 자동 생성된다.
/// </para>
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ArtifactIconBar : MonoBehaviour
{
    [Header("Runtime Source")]
    [SerializeField] private ArtifactManager artifactManager;
    [SerializeField] private bool findArtifactManagerOnEnable = true;

    [Header("Layout")]
    [SerializeField] private float iconSize = 100f;
    [SerializeField] private float iconSpacing = 8f;

    [Header("Text")]
    [SerializeField] private TMP_FontAsset sharedFont;
    [SerializeField] private float countFontSize = 14f;
    [SerializeField] private float tooltipFontSize = 16f;
    [SerializeField] private float tooltipWidth = 240f;
    [SerializeField] private float tooltipHeight = 90f;

    private RectTransform slotContainer;
    private RectTransform tooltip;
    private TextMeshProUGUI tooltipText;

    private readonly List<ArtifactIconSlot> slots = new();

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnArtifactProgressChanged += HandleProgressChanged;

        if (findArtifactManagerOnEnable && artifactManager == null)
            artifactManager = FindAnyObjectByType<ArtifactManager>();

        EnsureSlotContainer();
        EnsureTooltip();
        BuildSlots();
    }

    private void OnDisable()
    {
        GameEvents.OnArtifactProgressChanged -= HandleProgressChanged;
    }

    // ═══════════════════════════════════════════
    //  Public API (ArtifactIconSlot에서 호출)
    // ═══════════════════════════════════════════

    public void ShowTooltip(ArtifactData artifact)
    {
        if (artifact == null || tooltip == null) return;

        string requirementText = "";
        var requirements = artifact.Requirements;
        for (int i = 0; i < requirements.Count; i++)
        {
            requirementText += $"{requirements[i].color} {requirements[i].requiredCount}";
            if (i < requirements.Count - 1) requirementText += ", ";
        }

        tooltipText.SetText($"<b>{artifact.ArtifactName}</b>\n{artifact.Description}\n요구: {requirementText}");
        tooltip.gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltip != null)
            tooltip.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    private void HandleProgressChanged(ArtifactData artifact, SymbolType color, int current, int required)
    {
        foreach (var slot in slots)
        {
            if (slot.Artifact != artifact) continue;

            // 아이콘 1개당 요구 색상 1개를 대표로 표시한다 (현재 아티팩트는 전부 단일 색상 요구).
            bool isPrimaryColor = artifact.Requirements.Count > 0 && artifact.Requirements[0].color == color;
            if (isPrimaryColor)
                slot.SetCount(current, required);
        }
    }

    private void BuildSlots()
    {
        for (int i = slotContainer.childCount - 1; i >= 0; i--)
            Destroy(slotContainer.GetChild(i).gameObject);
        slots.Clear();

        if (artifactManager == null) return;

        foreach (var artifact in artifactManager.EquippedArtifacts)
        {
            if (artifact == null) continue;
            slots.Add(CreateSlot(artifact));
        }
    }

    private ArtifactIconSlot CreateSlot(ArtifactData artifact)
    {
        var go = new GameObject($"ArtifactIcon_{artifact.ArtifactName}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(slotContainer, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(iconSize, iconSize);

        var image = go.GetComponent<Image>();
        image.sprite = artifact.Icon;
        image.color = artifact.Requirements.Count > 0
            ? SymbolVisuals.GetColor(artifact.Requirements[0].color)
            : Color.gray;
        image.raycastTarget = true;

        var countGo = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        countGo.transform.SetParent(rect, false);

        var countRect = countGo.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = Vector2.zero;
        countRect.sizeDelta = new Vector2(iconSize, iconSize * 0.4f);

        var countText = countGo.GetComponent<TextMeshProUGUI>();
        ApplyFont(countText);
        countText.fontSize = countFontSize;
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.color = Color.white;
        countText.fontStyle = FontStyles.Bold;
        countText.raycastTarget = false;

        var slot = go.AddComponent<ArtifactIconSlot>();
        slot.Setup(this, artifact, countText);
        return slot;
    }

    private void EnsureSlotContainer()
    {
        if (slotContainer != null) return;

        Transform existing = transform.Find("SlotContainer");
        if (existing != null)
        {
            slotContainer = existing.GetComponent<RectTransform>();
            return;
        }

        var go = new GameObject("SlotContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(transform, false);

        slotContainer = go.GetComponent<RectTransform>();
        slotContainer.anchorMin = new Vector2(1f, 1f);
        slotContainer.anchorMax = new Vector2(1f, 1f);
        slotContainer.pivot = new Vector2(1f, 1f);
        slotContainer.anchoredPosition = Vector2.zero;

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = iconSpacing;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureTooltip()
    {
        if (tooltip != null) return;

        Transform existing = transform.Find("Tooltip");
        if (existing != null)
        {
            tooltip = existing.GetComponent<RectTransform>();
            tooltipText = tooltip.GetComponentInChildren<TextMeshProUGUI>(true);
            tooltip.gameObject.SetActive(false);
            return;
        }

        var go = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        tooltip = go.GetComponent<RectTransform>();
        tooltip.anchorMin = new Vector2(1f, 1f);
        tooltip.anchorMax = new Vector2(1f, 1f);
        tooltip.pivot = new Vector2(1f, 1f);
        tooltip.sizeDelta = new Vector2(tooltipWidth, tooltipHeight);
        // 아이콘 한 줄 바로 아래에 고정 표시
        tooltip.anchoredPosition = new Vector2(0f, -(iconSize + iconSpacing));

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(tooltip, false);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 8f);
        textRect.offsetMax = new Vector2(-8f, -8f);

        tooltipText = textGo.GetComponent<TextMeshProUGUI>();
        ApplyFont(tooltipText);
        tooltipText.fontSize = tooltipFontSize;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;

        tooltip.gameObject.SetActive(false);
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (text != null && sharedFont != null)
            text.font = sharedFont;
    }
}
