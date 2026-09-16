/// <summary>
/// 보상 화면에 뜨는 선택지 하나를 추상화한다. 지금은 CardRewardOption만 있지만,
/// 나중에 아티팩트/재화 보상을 추가할 때 이 인터페이스만 구현하면 RewardScreen/RewardGenerator는
/// 그대로 재사용할 수 있다.
/// </summary>
public interface IRewardOption
{
    // 보상 화면 버튼에 표시할 이름.
    string DisplayName { get; }

    // 보상 화면 버튼에 표시할 설명.
    string Description { get; }

    // 플레이어가 이 보상을 선택했을 때, RunState에 실제로 반영한다.
    void Grant(RunState run);
}
