using UnityEditor;
using UnityEngine;

[System.Serializable]
public class MonsterDTO 
{
    public string MonsterID;
    public string MonsterName;
    public int Level; //몬스터의 레벨
    public int HP;
    public float ATK; //몬스터의 공격 뎀지
    public float EXP; //몬스터 사망 시, 플레이어에게 줄 경험치
    public int DropGroupID;
}
public class MonsterDataImporter : BaseDataImporter<MonsterDTO, MonsterData>
{
    protected override string JsonFilePath => "/02_Data/Json/Monster/MonsterObj.json";
    public override string SOFolderPath => "Assets/02_Data/SO/Monster/";

    protected override void MapDTOToSO(MonsterDTO dto, MonsterData so)
    {
        so.MonsterID = dto.MonsterID;
        so.MonsterName = dto.MonsterName;
        so.Level = dto.Level;
        so.HP = dto.HP;
        so.ATK = dto.ATK;
        so.EXP = dto.EXP;
        so.DropGroupID = dto.DropGroupID;
    }

    protected override string GetDTOID(MonsterDTO dto)
    {
        return dto.MonsterID;
    }

    [MenuItem("Tools/MonsterData 가져오기")]
    public static void Import()
    {
        BaseDataImporter<MonsterDTO, MonsterData> importer = new MonsterDataImporter();
        importer.Import();
    }
}