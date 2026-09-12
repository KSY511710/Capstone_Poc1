using TMPro;
using UnityEngine;

/// <summary>
/// 이번 턴 동안 배치 즉시 적용된 공격/방어 누적치를 실시간으로 표시한다.
/// 카드 효과는 배치 즉시 발동하므로, 카드 결산(OnResolutionResult)과
/// 아티팩트 발동(OnOverlapEffectTriggered) 결과를 매번 누적해서 보여준다.
/// </summary>
public class DamagePreviewView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI defenseText;

    private int accumulatedDamage;
    private int accumulatedDefense;

    private void OnEnable()
    {
        GameEvents.OnResolutionResult       += HandleResolution;
        GameEvents.OnOverlapEffectTriggered += HandleResolution;
        GameEvents.OnDrawPhaseStarted       += HandleDrawPhaseStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnResolutionResult       -= HandleResolution;
        GameEvents.OnOverlapEffectTriggered -= HandleResolution;
        GameEvents.OnDrawPhaseStarted       -= HandleDrawPhaseStarted;
    }

    private void HandleResolution(ResolutionResult r)
    {
        accumulatedDamage  += r.damage;
        accumulatedDefense += r.defense;

        if (damageText != null)  damageText.text  = $"공격  {accumulatedDamage}";
        if (defenseText != null) defenseText.text = $"방어  {accumulatedDefense}";
    }

    private void HandleDrawPhaseStarted(int _)
    {
        accumulatedDamage = 0;
        accumulatedDefense = 0;

        if (damageText != null)  damageText.text  = "공격  0";
        if (defenseText != null) defenseText.text = "방어  0";
    }
}
