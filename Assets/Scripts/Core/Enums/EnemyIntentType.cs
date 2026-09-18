/// <summary>
/// 적이 다음 턴에 취할 행동의 종류. 적 의도(Intent) UI 표시에 사용된다.
/// </summary>
public enum EnemyIntentType
{
    Attack,  // 플레이어를 공격 (Value = 데미지)
    Defense, // 자신에게 방어도 부여 (Value = 방어도)
}
