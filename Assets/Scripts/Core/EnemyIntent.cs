/// <summary>
/// 적의 다음 턴 행동을 나타내는 값. UI가 이걸 받아 아이콘/숫자로 표시한다.
/// </summary>
public readonly struct EnemyIntent
{
    public readonly EnemyIntentType Type;
    public readonly int Value;

    public EnemyIntent(EnemyIntentType type, int value)
    {
        Type = type;
        Value = value;
    }
}
