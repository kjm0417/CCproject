using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public interface IZoneState
{
    void OnEnter(TerritoryZone zone);
    void OnExit(TerritoryZone zone);
}

public class LockedZoneState : IZoneState
{
    private TerritoryZoneUIManager uiManager;
    public LockedZoneState(TerritoryZoneUIManager uiManager)
    {
        this.uiManager = uiManager;
    }

    public void OnEnter(TerritoryZone zone)
    {
        zone.TerritoryTileMapGround.gameObject.layer = 6;
        zone.TerritoryTileMapWall.gameObject.layer = 6;
        zone.TerritoryTileMapWall.GetComponent<TilemapCollider2D>().enabled = true;
        zone.TerritoryZoneUIManager.ApplyLockedState();
    }

    public void OnExit(TerritoryZone zone) { }
}

public class UnlockingZoneState : IZoneState
{
    private TerritoryZoneUIManager uiManager;
    public UnlockingZoneState(TerritoryZoneUIManager uiManager)
    {
        this.uiManager = uiManager;
    }
    public void OnEnter(TerritoryZone zone)
    {
        zone.TerritoryTileMapGround.gameObject.layer = 8;
        zone.TerritoryTileMapWall.gameObject.layer = 8;
        zone.TerritoryZoneUIManager.ApplyUnlockingState();
    }

    public void OnExit(TerritoryZone zone)
    {
        zone.TerritoryZoneUIManager.HideFocusingImage();
    }
}

public class UnlockedZoneState : IZoneState
{
    private TerritoryZoneUIManager uiManager;
    public UnlockedZoneState(TerritoryZoneUIManager uiManager)
    {
        this.uiManager = uiManager;
    }
    public void OnEnter(TerritoryZone zone)
    {
        zone.TerritoryTileMapGround.gameObject.layer = 7;
        zone.TerritoryTileMapWall.gameObject.layer = 7;
        zone.TerritoryTileMapWall.GetComponent<TilemapCollider2D>().enabled = false;
        zone.TerritoryZoneUIManager.ApplyUnlockedState();
    }

    public void OnExit(TerritoryZone zone) { }
}

/// <summary>
/// 영역 하나를 대표하는 컴포넌트(데이터와 로직 담당)
/// </summary>
public class TerritoryZone : MonoBehaviour
{
    private IZoneState currentState;
    private IZoneState lockedState;
    private IZoneState unlockingState;
    private IZoneState unlockedState;

    #region 필요 데이터 선언
    [SerializeField]
    private TerritoryZoneData territoryZoneData;

    [SerializeField]
    TerritoryZoneUIManager territoryZoneUIManager;
    public TerritoryZoneUIManager TerritoryZoneUIManager => territoryZoneUIManager;

    [SerializeField]
    private Tilemap territoryTileMapGround, territoryTileMapWall;

    public Tilemap TerritoryTileMapGround => territoryTileMapGround;
    public Tilemap TerritoryTileMapWall => territoryTileMapWall;

    [SerializeField]
    private bool isLocked = true;

    public static Action<TerritoryZone, TerrirotyDirection> OnZoneApproached;
    public static Action OnZoneLeaved;
    public static Action<TerritoryZone> OnZoneBuyClick;
    public static event Action<bool> OnTerritoryUnlocked;

    public TerritoryZoneData TerritoryZoneData => territoryZoneData;

    public bool IsLocked => isLocked;

    #endregion

    private void Awake()
    {
        lockedState = new LockedZoneState(territoryZoneUIManager);
        unlockingState = new UnlockingZoneState(territoryZoneUIManager);
        unlockedState = new UnlockedZoneState(territoryZoneUIManager);

        if (isLocked)
            ChangeState(lockedState);
        else
            ChangeState(unlockedState);
    }

    private void ChangeState(IZoneState newState)
    {
        currentState?.OnExit(this);
        currentState = newState;
        currentState.OnEnter(this);
    }

    /// <summary>
    /// 인접한 구역의 해당하는 타일맵의 레이어 변경
    /// </summary>
    public void UpdateAdjacentZoneLayer()
    {
        territoryTileMapGround.gameObject.layer = 6;
        territoryTileMapWall.gameObject.layer = 6;
    }

    public void TryLockedZoneSet()
    {
        ChangeState(unlockingState);
    }

    public void TryUnLockedZoneSet()
    {
        ChangeState(lockedState);
    }

    /// <summary>
    /// 구역 해금 후 지역 상태를 변경하는 메서드
    /// </summary>
    public void UnLockZone()
    {
        isLocked = false;
        
        string[] objParts = this.gameObject.name.Split('_');
        this.gameObject.name = "Zone_" + objParts[1];

        StartCoroutine(TerritoryAlpha());
    }

    IEnumerator TerritoryAlpha()
    {
        Color c1 = territoryTileMapGround.color;
        Color c2 = territoryTileMapWall.color;
        float startAlpha1 = c1.a;
        float startAlpha2 = c2.a;
        float elapsed = 0f;

        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            c1.a = Mathf.Lerp(startAlpha1, 1f, elapsed / 1.0f);
            c2.a = Mathf.Lerp(startAlpha2, 1f, elapsed / 1.0f);
            territoryTileMapGround.color = c1;
            territoryTileMapWall.color = c2;
            yield return null;
        }

        c1.a = 1f;
        territoryTileMapGround.color = c1;
        c2.a = 1f;
        territoryTileMapWall.color = c2;

        ChangeState(unlockedState);

        GetComponent<ResourceSpawner>().SpawnInitial();

        OnTerritoryUnlocked?.Invoke(false);

        yield return new WaitForSeconds(1.5f);

        TerritoryDirectionManager.Instance.ResetBackClicked(this, true);
    }

    TerrirotyDirection currentZoneDir;

    /// <summary>
    /// 접근되지 않은 영역에 접근할 때 처리하는 메서드
    /// </summary>
    public void ApproachZone(TerritoryZone playerZone, TerrirotyDirection terrirotyDirection) 
    {
        currentZoneDir = terrirotyDirection;

        playerZone.territoryZoneUIManager.OnZoomOutViewed(playerZone, terrirotyDirection);
    }

    /// <summary>
    /// 접근되지 않은 영역을 떠날 때 처리하는 메서드
    /// </summary>
    public void LeaveZone()
    {
        OnZoneLeaved?.Invoke();
    }
}
