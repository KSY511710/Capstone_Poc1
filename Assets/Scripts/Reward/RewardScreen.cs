using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat 씬에 배치해두는 보상 선택 화면. 평소엔 비활성 상태로 있다가
/// CombatResultRelay가 승리 시 Show()를 호출하면 옵션들을 뿌려 보여주고,
/// 플레이어가 하나를 고르면 콜백으로 알린 뒤 스스로 닫힌다.
/// </summary>
public class RewardScreen : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("보상 화면 전체를 켜고 끌 루트 오브젝트 (보통 이 컴포넌트가 붙은 패널 자신)")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("옵션 뷰들이 생성될 부모")]
    [SerializeField] private Transform optionContainer;
    [Tooltip("옵션 하나를 표시할 프리팹")]
    [SerializeField] private RewardOptionView optionViewPrefab;

    private readonly List<RewardOptionView> spawnedViews = new();

    /// <summary>
    /// 보상 화면을 켜고 options를 전부 뿌려 보여준다. 하나가 선택되면 onChosen을 호출한 뒤 화면을 닫는다.
    /// </summary>
    public void Show(List<IRewardOption> options, Action<IRewardOption> onChosen)
    {
        ClearOptions();
        panelRoot.SetActive(true);

        foreach (IRewardOption option in options)
        {
            RewardOptionView view = Instantiate(optionViewPrefab, optionContainer);
            view.Setup(option, () =>
            {
                Hide();
                onChosen?.Invoke(option);
            });
            spawnedViews.Add(view);
        }
    }

    private void Hide()
    {
        ClearOptions();
        panelRoot.SetActive(false);
    }

    private void ClearOptions()
    {
        foreach (RewardOptionView view in spawnedViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        spawnedViews.Clear();
    }
}
