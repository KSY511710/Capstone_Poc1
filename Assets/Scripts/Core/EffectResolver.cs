/// <summary>
/// CardEffect를 ResolutionResult에 누적 적용하는 공용 로직.
/// GridManager(카드 결산)와 ArtifactManager(아티팩트 발동)가 공유한다.
/// </summary>
public static class EffectResolver
{
    public static void Apply(ref ResolutionResult result, CardEffect effect)
    {
        switch (effect.effectType)
        {
            case CardType.Attack:  result.damage  += effect.power; break;
            case CardType.Defense: result.defense += effect.power; break;
            case CardType.Heal:    result.heal    += effect.power; break;
            case CardType.Draw:    result.draw    += effect.power; break;
            case CardType.DrawNow: result.drawNow += effect.power; break;
            case CardType.Drain:
                result.damage += effect.power;
                result.heal   += effect.power;
                break;
            case CardType.WeakenEnemyAttack: result.enemyAttackReduction += effect.power; break;
        }
    }
}
