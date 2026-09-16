using System.Collections.Generic;

/// <summary>
/// CardPool과 seed를 받아 보상 선택지를 뽑아내는 정적 생성기.
/// MapGenerator와 동일하게 UnityEngine.Random이 아닌 별도 System.Random을 받아서
/// 전투 등 다른 곳의 난수 소비와 간섭하지 않게 한다.
/// </summary>
public static class RewardGenerator
{
    /// <summary>
    /// pool에서 중복 없이 최대 count장을 균등 확률로 뽑는다. pool이 count보다 작으면 있는 만큼만 반환한다.
    /// </summary>
    public static List<IRewardOption> GenerateCardOptions(CardPool pool, int count, System.Random random)
    {
        var result = new List<IRewardOption>();
        if (pool == null) return result;

        var remaining = new List<CardData>(pool.Cards);
        int pickCount = System.Math.Min(count, remaining.Count);

        for (int i = 0; i < pickCount; i++)
        {
            int index = random.Next(remaining.Count);
            result.Add(new CardRewardOption(remaining[index]));
            remaining.RemoveAt(index);
        }

        return result;
    }
}
