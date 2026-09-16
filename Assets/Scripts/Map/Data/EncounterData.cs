using UnityEngine;

/// <summary>
/// 전투 한 판의 적 구성을 정의 
/// 같은 Combat 씬을 일반 전투/엘리트/보스 전투로 다르게 만드는 실질적인 주체 —
/// 씬은 하나만 두고, 이 데이터를 CombatManager.SetEncounter(...)로 주입해서 적 강함을 바꾼다.
/// </summary>
[CreateAssetMenu(fileName = "NewEncounterData", menuName = "DeckBuilder/Map/Encounter Data")]
public class EncounterData : ScriptableObject
{
    // UI/로그 표시용 이름 (예: "숲의 늑대 무리", "엘리트: 강철 골렘", "1막 보스: ...").
    [SerializeField] private string encounterName = "New Encounter";

    // 이 조우에서 적의 최대 체력. CombatUnit.SetMaxHp로 주입된다.
    [SerializeField] private int enemyMaxHp = 30;

    // 이 조우에서 적의 기본 공격력. CombatManager.enemyBaseDamage로 주입된다.
    [SerializeField] private int enemyBaseDamage = 8;

    // 전투 UI에 표시할 적 초상화/스프라이트.
    [SerializeField] private Sprite enemySprite;

    public string EncounterName => encounterName;
    public int EnemyMaxHp => enemyMaxHp;
    public int EnemyBaseDamage => enemyBaseDamage;
    public Sprite EnemySprite => enemySprite;
}
