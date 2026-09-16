/// <summary>
/// 카드 자체의 분류. 카드 효과(CardType)와는 별개로, 카드가 어떤 종류인지 구분하는 용도.
/// 추후 UI 표시(아이콘, 색상 등)에 사용될 예정이며, 종류는 계속 추가될 수 있다.
/// </summary>
public enum CardCategory
{
    Attack, // 공격 카드
    Skill,  // 스킬 카드
}
