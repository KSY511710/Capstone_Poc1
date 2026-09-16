using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드(스킬) 한 장의 정적 데이터를 정의하는 ScriptableObject.
/// 블록 형태 데이터와 전투 속성(위력, 타입)을 모두 포함하여 단일 파일로 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewCardData", menuName = "DeckBuilder/Card Data")]
public class CardData : ScriptableObject
{
    [Header("카드 기본 정보")]
    [Tooltip("카드 이름 (UI 표시용)")]
    [SerializeField] private string cardName = "New Card";

    [Tooltip("카드 설명 (UI 표시용)")]
    [TextArea(2, 4)]
    [SerializeField] private string description = "";

    [Tooltip("카드 종류 — 카드 효과와는 별개로 카드 자체를 분류하는 용도. UI 표시는 추후 추가 예정.")]
    [SerializeField] private CardCategory category = CardCategory.Attack;

    [Header("효과 목록")]
    [Tooltip("배치 효과 — 그리드에 배치되는 즉시 적용된다.")]
    [SerializeField] private List<CardEffect> effects = new();

    [HideInInspector]
    [SerializeField] private int width = 1;

    [HideInInspector]
    [SerializeField] private int height = 1;

    [HideInInspector]
    [SerializeField] private SymbolType[] symbols;

    // ── Public Properties ──
    public string CardName => cardName;
    public string Description => description;
    public CardCategory Category => category;
    public IReadOnlyList<CardEffect> Effects => effects;

    // {0} → effects[0].power, {1} → effects[1].power 치환
    public string FormattedDescription
    {
        get
        {
            string text = description;
            for (int i = 0; i < effects.Count; i++)
                text = text.Replace("{" + i + "}", effects[i].power.ToString());
            return text;
        }
    }

    // 첫 번째 효과 기준 — 기존 코드 호환용
    public CardType Type    => effects.Count > 0 ? effects[0].effectType : CardType.Attack;
    public int      BasePower => effects.Count > 0 ? effects[0].power    : 0;
    
    public int Width => width;
    public int Height => height;

    /// <summary>
    /// 특정 좌표(col, row)의 문양을 반환한다.
    /// 범위를 벗어나면 <see cref="SymbolType.None"/>을 반환한다.
    /// </summary>
    public SymbolType GetSymbol(int col, int row)
    {
        if (col < 0 || col >= width || row < 0 || row >= height)
            return SymbolType.None;

        int index = row * width + col;
        if (index < 0 || index >= symbols?.Length)
            return SymbolType.None;

        return symbols[index];
    }

    /// <summary>
    /// 블록이 실제로 차지하는 칸(Symbol != None)의 좌표 목록을 반환한다.
    /// 배치 검증이나 겹치기 판별 시 반복 순회에 사용된다.
    /// </summary>
    public (int col, int row, SymbolType symbol)[] GetOccupiedCells()
    {
        if (symbols == null) return System.Array.Empty<(int, int, SymbolType)>();

        // 일단 최대 크기로 할당 후, 실제 개수만 복사
        var buffer = new (int, int, SymbolType)[width * height];
        int count = 0;

        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                SymbolType sym = GetSymbol(c, r);
                if (sym != SymbolType.None)
                {
                    buffer[count++] = (c, r, sym);
                }
            }
        }

        var result = new (int, int, SymbolType)[count];
        System.Array.Copy(buffer, result, count);
        return result;
    }

    /// <summary>
    /// 시계방향으로 90도 × rotationSteps만큼 회전시킨 상태의 차지 칸 좌표 목록을 반환한다.
    /// 첫 번째 칸(맨 위·맨 왼쪽 칸)을 회전 기준점으로 고정하고 나머지 칸이 그 주위를 돈다 —
    /// 그래서 회전해도 기준 칸은 항상 마우스(그리드 배치 기준점) 아래 그대로 남는다.
    /// 좌표는 기준 칸을 (0,0)으로 하는 상대좌표이며 음수가 나올 수 있다.
    /// 원본 데이터는 건드리지 않는다 (드래그 중 QE 회전에 사용).
    /// </summary>
    public (int col, int row, SymbolType symbol)[] GetOccupiedCells(int rotationSteps)
    {
        var baseCells = GetOccupiedCells();
        if (baseCells.Length == 0) return baseCells;

        int steps = ((rotationSteps % 4) + 4) % 4;

        // steps가 0이어도 기준 칸을 (0,0)으로 맞춘 상대좌표로 반환해야
        // 다른 회전값과 좌표계가 일치해서 회전 시 블록이 튀지 않는다.
        int pivotCol = baseCells[0].col;
        int pivotRow = baseCells[0].row;

        var result = new (int, int, SymbolType)[baseCells.Length];
        for (int i = 0; i < baseCells.Length; i++)
        {
            int dc = baseCells[i].col - pivotCol;
            int dr = baseCells[i].row - pivotRow;

            for (int s = 0; s < steps; s++)
            {
                int newDc = -dr;
                int newDr = dc;
                dc = newDc;
                dr = newDr;
            }

            result[i] = (dc, dr, baseCells[i].symbol);
        }
        return result;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (width < 1) width = 1;
        if (height < 1) height = 1;

        int requiredLength = width * height;
        if (symbols == null || symbols.Length != requiredLength)
        {
            var old = symbols ?? System.Array.Empty<SymbolType>();
            symbols = new SymbolType[requiredLength];
            System.Array.Copy(old, symbols, Mathf.Min(old.Length, requiredLength));
        }
    }
#endif
}
