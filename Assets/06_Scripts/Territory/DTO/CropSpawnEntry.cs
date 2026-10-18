/// <summary>
/// 구역 해금 시 다 자란 상태로 심어둘 작물 정보 (흙 1칸 + 작물 1포기 = 1개)
/// </summary>
[System.Serializable]
public class CropSpawnEntry
{
    public InvenItemData Seed; //심을 씨앗 (CropPlanter 매핑으로 작물 결정)

    public int MaxCount; //심어둘 포기 수
}
