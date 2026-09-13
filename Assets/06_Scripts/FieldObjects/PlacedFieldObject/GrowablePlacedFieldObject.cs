// 성장 단계가 필요한 플레이어 배치 오브젝트는 이 클래스를 기준으로 확장한다.
public class GrowablePlacedFieldObject : PlacedFieldObject, IGrowable
{
    public virtual int GrowthStage => 0;
    public virtual bool IsHarvestable => false;
}
