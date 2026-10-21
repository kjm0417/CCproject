using UnityEngine;

/// <summary>
/// 던전 기믹용 씨앗 ( 3층 슬라임 드랍 -> 같은 색 씨앗 타일에만 심기 )
/// 일반 씨앗과 달리 영지 작물 매핑 없음 / 던전 퇴장 시 인벤토리에서 제거
/// </summary>
[CreateAssetMenu(fileName = "GimmickSeed", menuName = "Game/GimmickSeedData")]
public class GimmickSeedData : InvenItemData
{
}
