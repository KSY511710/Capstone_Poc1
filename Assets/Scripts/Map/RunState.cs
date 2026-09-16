using System;
using System.Collections.Generic;

[Serializable]

/// <summary>
/// 게임 진행 상태 저장하는 순수 데이터 클래스
/// GameManager 가 DontDestroyOnLoad로 들고있어서 지속 유지
/// Json 저장 확장성 유지
/// </summary>
public class RunState
{
    public MapGraph currentMap; //이번 런에서 생성해둔 맵 그래프 
    public int floorNumber; //현재 플레이어 도달 층 번호 
    public int currency; //재화 골드 
    public int playerCurrentHp; // 플레이어 현재 체력
    public int playerMaxHp; //플레이어 최대 체력
    public List<CardData> deck = new(); //현재 런 플레이어 보유 덱
    public List<ArtifactData> artifacts = new(); // 현재 런 플레이어 보유 아티펙프
    public EncounterData pendingEncounter; // 방금 선택한 노드의 조우 데이터 — Combat 씬 진입 시 CombatManager.SetEncounter로 주입
}