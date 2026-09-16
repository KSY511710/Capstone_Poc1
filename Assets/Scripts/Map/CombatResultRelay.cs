using UnityEngine;

/// <summary>
/// Combat 씬에 배치해두는 중계용 컴포넌트
/// GameEvents.OnCombatEnded를 구독해서 전투 종료를 감지하고, 승패에 따른 후속 처리 전투 씬 쪽 책임
/// — GameManager는 "맵으로 돌아간다"는 사실만 알면 되고 승패 자체는 몰라도 된다.
/// </summary>
public class CombatResultRelay : MonoBehaviour
{
    [Header("승리 보상")]
    [SerializeField] private CardPool cardPool;
    [SerializeField] private RewardScreen rewardScreen;
    [Tooltip("보상 화면에 띄울 카드 선택지 수")]
    [SerializeField] private int rewardCardCount = 3;

    private void OnEnable() => GameEvents.OnCombatEnded += HandleCombatEnded;
    private void OnDisable() => GameEvents.OnCombatEnded -= HandleCombatEnded;

    /// <summary>
    /// GameEvents.OnCombatEnded가 발행될 때 호출되는 핸들러.
    /// </summary>
    /// <param name="win">전투 승리 여부</param>
    private void HandleCombatEnded(bool win)
    {
        if (win)
        {
            var options = RewardGenerator.GenerateCardOptions(cardPool, rewardCardCount, new System.Random());
            rewardScreen.Show(options, chosen =>
            {
                chosen.Grant(GameManager.Instance.Run);
                GameManager.Instance.ReturnToMap();
            });
        }
        else
        {
            // TODO: 게임오버 처리(런 종료, 결과 화면 표시 등)를 여기에 연결.
            // 아직 게임오버 씬/로직이 없어 우선 로그만 남기고, 패배 시에는 맵으로 돌아가지 않는다.
            Debug.Log("[CombatResultRelay] 패배 — 게임오버 처리 필요(아직 미구현)");
        }
    }
}
