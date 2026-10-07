using UnityEngine;

/// <summary>
/// 던전 층 1개 ( 층 루트 오브젝트에 부착 )
/// 한 층에 맵이 여러 개일 수 있으므로 층 번호 + 맵 ID로 구분
/// </summary>
public class DungeonMap : MonoBehaviour
{
    [SerializeField, Tooltip("이 맵이 속한 층")]
    private int floor = 1;
    [SerializeField, Tooltip("같은 층 안에서 맵 구분용 ID ( 예 : 2F_A, 2F_B )")]
    private string mapID;
    [SerializeField, Tooltip("포탈로 들어왔을 때 도착 위치. 비우면 맵 위치")]
    private Transform arrivalPoint;
    [SerializeField, Tooltip("이 맵 카메라 바운드 영역. 비우면 자식 중 이름이 CamBound 인 콜라이더 사용")]
    private Collider2D camBound;
    [SerializeField, Tooltip("보스 맵 - 카메라가 플레이어를 따라가지 않고 CamBound 중앙에 고정")]
    private bool isBossMap;

    private const string CamBoundName = "CamBound";

    public int Floor => floor;
    public string MapID => mapID;
    public Transform ArrivalPoint => arrivalPoint != null ? arrivalPoint : transform;
    public bool IsBossMap => isBossMap;
    public bool HasArrivalPoint => arrivalPoint != null;

    public Collider2D CamBound
    {
        get
        {
            if (camBound == null)
            {
                foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
                {
                    if (col.name == CamBoundName)
                    {
                        camBound = col;
                        break;
                    }
                }
            }
            return camBound;
        }
    }
}
