using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

#region 플레이어 저장 데이터 ( 체력 / 배고픔 / 레벨 )
/// <summary>
/// 씬 이동 또는 게임 종료 시 사라지는 플레이어 저장 데이터 ( 플레이어 상태 / 배고픔 / 레벨최고 하고, 인벤토리는 제외 )
/// </summary>
[System.Serializable]
public class PlayerSaveData
{
    //PlayerVitals 클래스
    public float HP; //체력
    public float HungerValue; //배고픔

    //PlayerProgression 클래스
    public int PlayerLevel; //플레이어 레벨
    public int ExpLevel; //경험치 레벨 또는 량
    public int SkillPoints; //플레이어 스킬 포인트

    //PlayerMovement 클래스
    public Vector2 Position; //플레이어 위치
    public bool FlipX; //플레이어 방향

    //PlayerBaseStatsData 클래스
    public float MoveSpeed;   // 이동 속도
    public float AttackPower; // 공격력
    public float AttackRange;  // 공격 범위
    public float MaxHp;      // 최대 체력

    //PlayerWallet 클래스
    public List<CurrencyAmount> currencies = new();

    /// <summary>
    /// 플레이어 데이터 저장
    /// </summary>
    /// <param name="playerContext"></param>
    /// <returns></returns>
    public static PlayerSaveData Save(PlayerContext playerContext)
    {
        PlayerSaveData data = new PlayerSaveData();

        data.HP = playerContext.Vitals.Hp;
        data.HungerValue = playerContext.Vitals.HungerValue;
        data.PlayerLevel = playerContext.Progression.Level;
        data.ExpLevel = playerContext.Progression.ExpInLevel;
        data.SkillPoints = playerContext.Progression.SkillPoints;

        data.Position = playerContext.Movement.transform.position;
        data.FlipX = playerContext.SpriteRenderer.flipX;

        data.MoveSpeed = playerContext.BaseData.MoveSpeed;
        data.AttackPower = playerContext.BaseData.AttackPower;
        data.AttackRange = playerContext.BaseData.AttackRange;
        data.MaxHp = playerContext.BaseData.MaxHp;


        foreach(var c in playerContext.Wallet.Balances)
        {
            data.currencies.Add(new CurrencyAmount
            {
                Type = c.Key,
                Amount = c.Value

            });
        }

        return data;
    }

    /// <summary>
    /// 게임 로드 시 데이터를 로드 ( 위치는 제외한다고 로드않는다 )
    /// </summary>
    /// <param name="data"></param>
    /// <param name="playerContext"></param>
    public static void Load(PlayerSaveData data , PlayerContext playerContext)
    {
        //PlayerVitals 로드
        playerContext.Vitals.Load(data.HP, data.HungerValue);

        //PlayerProgression 로드
        playerContext.Progression.Load(data.PlayerLevel, data.ExpLevel, data.SkillPoints);

        //PlayerMovement 로드는 씬 로드 이후에 따로 하지 않는다.

        //PlayerBaseStatsData 로드
        playerContext.Stats.Load(data.MoveSpeed,data.AttackPower,data.AttackRange,data.MaxHp);

        //PlayerWallet 로드
        playerContext.Wallet.Load(data.currencies);
    }
}
#endregion

[System.Serializable]
public class TerritorySaveData
{
    public List<int> UnlockedZoneIds;
    public List<FieldObjectStateData> fieldObjects;
    public List<RespawningResourceData> respawningResources = new List<RespawningResourceData>();
}

[System.Serializable]
public class FieldObjectStateData
{
    public int ZoneId;
    public int ResourceEntryIndex;
    public int CurrentHp;
    public bool IsDepleted;
    public int GrowthStage;
    public Vector2 Position;
    public string ResourcePrefabName;
}

[System.Serializable]
public class RespawningResourceData
{
    public int ZoneId;
    public string ResourcePrefabName;
    public float RemainingTime;
}

[System.Serializable]
public class RespawningResourceState
{
    public int ZoneId;
    public string ResourcePrefabName;
    public Vector2 Position;
    public float RemainingTime;
}
