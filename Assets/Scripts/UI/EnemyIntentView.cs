using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적의 다음 턴 행동(공격/방어)을 아이콘+숫자로 미리 보여주는 UI.
/// 씬에 미리 배치해두고(적 HP바 근처) 아이콘/텍스트를 인스펙터로 연결한다.
/// 스프라이트를 비워두면 색상만으로 공격/방어를 구분해 표시한다.
/// </summary>
public class EnemyIntentView : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI valueText;

    [Header("아이콘 (선택 사항 — 비워두면 색상만 사용)")]
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private Sprite defenseSprite;

    [Header("색상")]
    [SerializeField] private Color attackColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color defenseColor = new Color(0.4f, 0.7f, 1f);

    private void OnEnable()
    {
        GameEvents.OnEnemyIntentChanged += HandleIntentChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyIntentChanged -= HandleIntentChanged;
    }

    private void HandleIntentChanged(EnemyIntent intent)
    {
        bool isAttack = intent.Type == EnemyIntentType.Attack;

        if (icon != null)
        {
            icon.sprite = isAttack ? attackSprite : defenseSprite;
            icon.color = isAttack ? attackColor : defenseColor;
        }

        if (valueText != null)
            valueText.text = isAttack ? intent.Value.ToString() : $"+{intent.Value}";
    }
}
