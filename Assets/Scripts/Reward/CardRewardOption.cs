/// <summary>
/// 카드 하나를 보상 선택지로 감싼다. 선택되면 해당 카드를 RunState.deck에 추가한다.
/// </summary>
public class CardRewardOption : IRewardOption
{
    private readonly CardData card;

    public CardRewardOption(CardData card)
    {
        this.card = card;
    }

    public string DisplayName => card.CardName;
    public string Description => card.FormattedDescription;

    public void Grant(RunState run) => run.deck.Add(card);
}
