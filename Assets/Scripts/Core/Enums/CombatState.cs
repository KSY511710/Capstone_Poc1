/// <summary>
/// 턴 상태 기계의 상태 목록.
/// CombatManager가 이 상태를 순차적으로 전이하며 전투 루프를 제어한다.
/// </summary>
public enum CombatState
{
    None,           // 전투 시작 전 또는 종료 후
    PlayerDraw,     // 플레이어 드로우 페이즈
    Placement,      // 블록 배치 페이즈
    Discard,        // 손패 초과 시 버릴 카드 선택 대기
    EnemyTurn,      // 적 턴 페이즈
    Win,            // 승리
    Lose,           // 패배
}
