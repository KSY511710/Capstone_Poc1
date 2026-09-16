using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 승리 보상으로 뽑힐 수 있는 카드 후보 목록을 담는 ScriptableObject.
/// RewardGenerator.GenerateCardOptions가 이 풀에서 가중치 없이 균등하게 N장을 중복 없이 뽑는다.
/// </summary>
[CreateAssetMenu(fileName = "NewCardPool", menuName = "DeckBuilder/Reward/Card Pool")]
public class CardPool : ScriptableObject
{
    [SerializeField] private List<CardData> cards = new();

    public IReadOnlyList<CardData> Cards => cards;
}
