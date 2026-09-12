using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cyg.UI
{
    [DisallowMultipleComponent]
    public sealed class CygCombatStatusView : MonoBehaviour
    {
        private enum HpTextMode
        {
            CurrentOnly,
            CurrentSlashMax
        }

        [Header("Optional Runtime Sources")]
        [SerializeField] private CombatUnit playerUnit;
        [SerializeField] private CombatUnit enemyUnit;
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private bool findUnitsOnEnable = true;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private TextMeshProUGUI enemyHpText;
        [SerializeField] private TextMeshProUGUI playerDefenseText;
        [SerializeField] private TextMeshProUGUI playerAttackText;
        [SerializeField] private TextMeshProUGUI enemyAttackText;
        [SerializeField] private HpTextMode hpTextMode = HpTextMode.CurrentOnly;

        [Header("Optional Bars")]
        [SerializeField] private Image playerHpFill;
        [SerializeField] private Image enemyHpFill;

        [Header("Fallback")]
        [SerializeField] private string missingValueText = "--";

        private int accumulatedDamage = 0;

        private void OnEnable()
        {
            GameEvents.OnPlayerHpChanged  += HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged   += HandleEnemyHpChanged;
            GameEvents.OnPlayerDefenseChanged += HandlePlayerDefenseChanged;
            GameEvents.OnDrawPhaseStarted       += HandleDrawPhaseStarted;
            GameEvents.OnResolutionResult       += HandleResolutionResult;
            GameEvents.OnOverlapEffectTriggered += HandleResolutionResult;

            if (findUnitsOnEnable)
                RefreshUnitReferences();

            RefreshSnapshot();
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHpChanged  -= HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged   -= HandleEnemyHpChanged;
            GameEvents.OnPlayerDefenseChanged -= HandlePlayerDefenseChanged;
            GameEvents.OnDrawPhaseStarted       -= HandleDrawPhaseStarted;
            GameEvents.OnResolutionResult       -= HandleResolutionResult;
            GameEvents.OnOverlapEffectTriggered -= HandleResolutionResult;
        }

        public void RefreshUnitReferences()
        {
            CombatUnit[] units = FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i].IsPlayer) playerUnit = units[i];
                else                   enemyUnit  = units[i];
            }

            if (combatManager == null)
                combatManager = FindAnyObjectByType<CombatManager>();
        }

        public void RefreshSnapshot()
        {
            if (playerUnit != null)
            {
                HandlePlayerHpChanged(playerUnit.CurrentHp, playerUnit.MaxHp);
                HandlePlayerDefenseChanged(playerUnit.Defense);
            }
            else
            {
                SetText(playerHpText, missingValueText);
                SetFill(playerHpFill, 0f);
                SetText(playerDefenseText, missingValueText);
            }

            if (enemyUnit != null)
                HandleEnemyHpChanged(enemyUnit.CurrentHp, enemyUnit.MaxHp);
            else
            {
                SetText(enemyHpText, missingValueText);
                SetFill(enemyHpFill, 0f);
            }

            RefreshAttackTexts();
        }

        private void RefreshAttackTexts()
        {
            SetText(playerAttackText, accumulatedDamage.ToString());

            string enemyAtk = combatManager != null
                ? combatManager.EnemyBaseDamage.ToString()
                : missingValueText;
            SetText(enemyAttackText, enemyAtk);
        }

        // 카드 효과는 배치 즉시 발동하므로, 이번 턴 동안 실제로 적용된 데미지를 누적해서 보여준다.
        private void HandleDrawPhaseStarted(int _)
        {
            accumulatedDamage = 0;
            RefreshAttackTexts();
        }

        private void HandleResolutionResult(ResolutionResult result)
        {
            accumulatedDamage += result.damage;
            SetText(playerAttackText, accumulatedDamage.ToString());
        }

        private void HandlePlayerHpChanged(int current, int max)
        {
            SetHpText(playerHpText, current, max);
            SetHpFill(playerHpFill, current, max);
        }

        private void HandleEnemyHpChanged(int current, int max)
        {
            SetHpText(enemyHpText, current, max);
            SetHpFill(enemyHpFill, current, max);
        }

        private void HandlePlayerDefenseChanged(int defense)
        {
            SetText(playerDefenseText, defense.ToString());
        }

        private void SetHpText(TextMeshProUGUI target, int current, int max)
        {
            if (target == null)
            {
                return;
            }

            string text = hpTextMode == HpTextMode.CurrentSlashMax
                ? $"{current}/{max}"
                : current.ToString();

            target.SetText(text);
        }

        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target != null)
            {
                target.SetText(text);
            }
        }

        private static void SetHpFill(Image target, int current, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
            SetFill(target, ratio);
        }

        private static void SetFill(Image target, float value)
        {
            if (target != null)
            {
                target.fillAmount = value;
            }
        }
    }
}
