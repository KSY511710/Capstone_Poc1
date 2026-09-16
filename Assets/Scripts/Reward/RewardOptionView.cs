using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 화면에 뜨는 선택지 하나(카드 등)의 UI. MapNodeView와 같은 패턴 —
/// 데이터/콜백만 받아서 표시하고, 클릭 시 RewardScreen에 알리기만 한다.
/// </summary>
public class RewardOptionView : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    private Action onChosen;

    /// <summary>
    /// 이 뷰가 표현할 보상 선택지와, 클릭됐을 때 호출할 콜백을 지정한다.
    /// </summary>
    public void Setup(IRewardOption option, Action onChosen)
    {
        this.onChosen = onChosen;

        if (nameText != null) nameText.text = option.DisplayName;
        if (descriptionText != null) descriptionText.text = option.Description;

        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick() => onChosen?.Invoke();
}
