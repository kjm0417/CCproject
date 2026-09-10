using UnityEngine;

// 플레이어 기본 스탯 값(데이터). PlayerStats가 시작 시 이걸 읽어 기본값으로 쓴다.
// 밸런싱은 이 애셋 숫자만 바꾸면 되고, 캐릭터/직업별로 애셋을 갈아끼울 수 있다.
[CreateAssetMenu(fileName = "PlayerBaseStatsData", menuName = "Scriptable Objects/PlayerBaseStatsData")]
public class PlayerBaseStatsData : ScriptableObject
{
    public float MoveSpeed = 5f;   // 이동 속도
    public float AttackPower = 10f; // 공격력
    public float AttackRange = 2f;  // 공격 범위
    public float MaxHp = 100f;      // 최대 체력
}
