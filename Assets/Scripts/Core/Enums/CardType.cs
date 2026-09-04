/// <summary>
/// 카드(스킬 블록)의 효과 유형.
/// 결산 시 데미지 적용 대상이나 방어도 부여 등 분기에 사용된다.
/// </summary>
public enum CardType
{
    // ── 결산 효과 (턴 종료 시 발동) ──
    Attack,     // 적에게 데미지
    Defense,    // 자신에게 방어도 부여
    Heal,       // 자신 HP 회복
    Drain,      // 적에게 데미지 + 자신 HP 회복
    Draw,       // 다음 턴 추가 드로우 (power = 드로우 매수)
    DrawNow,    // 즉시 드로우 (power = 드로우 매수)
    WeakenEnemyAttack, // 다음 적 턴의 공격력을 감소시킴 (power = 감소량, 1회성)
}
