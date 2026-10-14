using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 던전 전투 시뮬레이터 ( 레벨 디자인 밸런스 확인용 )
/// 열린 던전 씬의 DungeonSpring / 적 프리팹 / 보스 데이터를 읽어 몬테카를로 방식으로 전투를 반복 계산
/// 사용 순서 : 데이터 불러오기 -> 시뮬레이션 실행 -> ( 필요 시 ) 추천값 계산 -> 재실행 확인 -> SO 에 적용
/// 메뉴 : Tools/전투 시뮬레이터
/// </summary>
public class CombatSimulatorWindow : EditorWindow
{
    #region 데이터 정의

    /// <summary>
    /// 플레이어 숙련도 가정 ( 명중률 / 회피율 / 접근 시간 / 보스 딜 가능 비율 )
    /// </summary>
    [Serializable]
    public class SkillProfile
    {
        public string Name;
        [Range(0f, 1f)] public float Accuracy;          //공격 시도 중 실제 명중 비율
        [Range(0f, 1f)] public float DodgeEnemy;        //일반 적 공격 회피율
        [Range(0f, 1f)] public float DodgeBoss;         //보스 패턴 회피율
        public float ApproachTime;                      //다음 적에게 붙는 데 걸리는 시간 ( 초 )
        [Range(0f, 1f)] public float BossUptimeIdle;    //보스 대기 중 딜 가능 비율
        [Range(0f, 1f)] public float BossUptimePattern; //보스 패턴 중 딜 가능 비율

        public SkillProfile(string name, float acc, float dodgeEnemy, float dodgeBoss, float approach, float upIdle, float upPattern)
        {
            Name = name;
            Accuracy = acc;
            DodgeEnemy = dodgeEnemy;
            DodgeBoss = dodgeBoss;
            ApproachTime = approach;
            BossUptimeIdle = upIdle;
            BossUptimePattern = upPattern;
        }
    }

    /// <summary>
    /// MonsterData 1개 ( 여러 프리팹이 공유 가능 ) - 표에서 수정하는 값
    /// </summary>
    private class MonsterEntry
    {
        public MonsterData Data;
        public string Users = "";
        public float HP;
        public float ATK;
        public int TargetHits = 3; //추천 계산 시 목표 처치 타수
    }

    /// <summary>
    /// 적 프리팹 1개 - 공격 쿨타임 정보
    /// </summary>
    private class EnemyProto
    {
        public string Name;
        public MonsterEntry M;
        public float BaseCd = 3f;
        public float SkillCd = 6f;
        public float StunAfterSkill = 2f;
    }

    /// <summary>
    /// 보스 패턴 1개 - 지속 시간 / 피격 기회는 코드 수치로 추정한 값 ( 표에서 수정 가능 )
    /// </summary>
    private class PatternEntry
    {
        public BossPattern Source;
        public string Name;
        public float Weight = 1f;
        public float Cooldown;
        public float Duration = 3f;
        public float Damage;
        public int HitChances = 1;      //패턴 1회에 플레이어가 맞을 수 있는 기회 수
        public float ExtraTickChance;   //맞았을 때 한 번 더 맞을 확률 ( 틱 피해 )
        public bool HasDamageField;
    }

    private enum FloorType { Room, NoCombat, Fountain, Boss }

    private class FloorVariant
    {
        public string MapName;
        public List<EnemyProto> Enemies = new List<EnemyProto>();
        public float Inc, Dec, Respawn; //4층 정화 증가 / 감소 속도, 리스폰 대기
        public int Points;              //4층 스폰 위치 수
        public int Index = -1;          //DungeonSpring.maps4F 인덱스 ( 적용용 )
    }

    private class FloorDef
    {
        public string Name;
        public FloorType Type;
        public Func<float> FixedTime = () => 0f; //전투 외 소요 시간 ( 발판 / 씨앗 심기 등 ) - 설정 변경 즉시 반영
        public List<FloorVariant> Variants = new List<FloorVariant>();
    }

    private class EnemyRt
    {
        public EnemyProto P;
        public float Hp, BaseT, SkillT, Stun;
        public bool Arrived, Dead;
        public float ArriveAt, RespawnAt;
    }

    private class Ctx
    {
        public System.Random Rng;
        public SkillProfile P;
        public float Hp, MaxHp, Hunger, StarveAcc;
        public bool Starved;

        public float R() => (float)Rng.NextDouble();
        public float Range(float a, float b) => a + (b - a) * R();
    }

    /// <summary>
    /// 숙련도 1개에 대한 반복 결과 집계
    /// </summary>
    private class Summary
    {
        public int Runs, Clears, Stuck, Starved, ReachedBoss, BossKills;
        public float[] FloorTime, FloorHpLost;
        public int[] FloorReached, FloorDeaths;
        public float SumTotalTime, SumBossEntryHp, SumBossEntryHunger, SumBossTime;

        public Summary(int floorCount)
        {
            FloorTime = new float[floorCount];
            FloorHpLost = new float[floorCount];
            FloorReached = new int[floorCount];
            FloorDeaths = new int[floorCount];
        }

        public float ClearRate => Runs > 0 ? (float)Clears / Runs : 0f;
        public float AvgBossEntryHp => ReachedBoss > 0 ? SumBossEntryHp / ReachedBoss : 0f;
        public float AvgBossEntryHunger => ReachedBoss > 0 ? SumBossEntryHunger / ReachedBoss : 0f;
        public float AvgBossTime => ReachedBoss > 0 ? SumBossTime / ReachedBoss : 0f;
    }

    #endregion

    #region 설정값

    [SerializeField] private PlayerBaseData playerData;
    [SerializeField] private float playerAtk = 10f, playerHp = 100f, playerCd = 0.4f;
    [SerializeField] private float hungerDrain = 0.5f, maxHunger = 100f, starveDamage = 1f, starveInterval = 1f;

    [SerializeField] private SkillProfile[] profiles =
    {
        new SkillProfile("초보", 0.55f, 0.25f, 0.45f, 1.5f, 0.6f, 0.2f),
        new SkillProfile("보통", 0.70f, 0.45f, 0.65f, 1.0f, 0.75f, 0.3f),
        new SkillProfile("숙련", 0.85f, 0.65f, 0.85f, 0.6f, 0.9f, 0.45f),
    };

    [SerializeField] private int runs = 500;
    [SerializeField] private int seed = 1234;
    [SerializeField, Tooltip("현재 타겟이 아닌 적의 공격이 사거리 안에서 나올 확률")]
    private float offTargetPressure = 0.5f;
    [SerializeField] private float time2F = 25f;
    [SerializeField] private float time3FExtra = 15f;
    [SerializeField] private float travel4F = 3f;
    [SerializeField, Tooltip("무적 보스 ( 스턴 시에만 피해 ) 일 때 스턴 기믹 성공 주기 / 지속")]
    private float stunCycle = 30f, stunDuration = 8f;
    [SerializeField] private float maxFloorTime = 600f;

    [SerializeField] private int targetProfile = 1;
    [SerializeField, Range(0.1f, 1f)] private float targetBossEntryHp = 0.6f;
    [SerializeField] private float targetBossTime = 90f;
    [SerializeField, Range(0.05f, 1f)] private float targetBossClear = 0.75f;
    [SerializeField] private float targetFountainTime = 60f;

    #endregion

    #region 불러온 데이터 ( 씬 참조라 도메인 리로드 시 다시 불러오기 필요 )

    private DungeonSpring dungeon;
    private BossData bossData;
    private float bossHp;
    private bool bossInvulnerable;
    private float bossStunMul = 1f;
    private Vector2 bossIdle = new Vector2(1.5f, 2.5f);
    private bool bossAvoidRepeat = true;
    private bool hasStunGimmick;

    private readonly List<MonsterEntry> monsters = new List<MonsterEntry>();
    private readonly List<PatternEntry> patterns = new List<PatternEntry>();
    private readonly List<FloorDef> floors = new List<FloorDef>();
    private bool loaded;

    //추천값 계산 중 임시 배율
    private float simEnemyAtkMul = 1f;
    private float simBossDmgMul = 1f;

    private string report = "";
    private Vector2 scroll;
    private bool foldProfiles, foldSettings, foldTargets = true;

    #endregion

    [MenuItem("Tools/전투 시뮬레이터")]
    private static void Open()
    {
        GetWindow<CombatSimulatorWindow>("전투 시뮬레이터").minSize = new Vector2(520f, 500f);
    }

    #region GUI

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox("던전 씬 ( DungeonSpring 이 있는 씬 ) 을 연 상태에서 '데이터 불러오기'를 누르세요.\n표의 값은 시뮬레이션용 가정값이며 'SO 에 적용' 전까지 실제 데이터는 바뀌지 않습니다.", MessageType.Info);

        if (GUILayout.Button("데이터 불러오기 ( 현재 씬 )", GUILayout.Height(26f)))
        {
            LoadFromScene();
        }

        if (!loaded)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawPlayer();
        DrawMonsters();
        DrawFountain();
        DrawBoss();
        DrawProfiles();
        DrawSettings();
        DrawTargets();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("시뮬레이션 실행", GUILayout.Height(30f)))
            {
                report = BuildReport();
            }
            if (GUILayout.Button("추천값 계산 ( 목표 기준 )", GUILayout.Height(30f)))
            {
                Recommend();
                report = BuildReport();
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("결과 복사"))
            {
                EditorGUIUtility.systemCopyBuffer = report;
            }
            if (GUILayout.Button("SO 에 적용 ( 적 HP/ATK · 보스 HP · 패턴 피해 )"))
            {
                ApplyToAssets();
            }
        }

        if (!string.IsNullOrEmpty(report))
        {
            EditorGUILayout.Space();
            GUIStyle style = new GUIStyle(EditorStyles.textArea) { font = EditorStyles.miniFont, wordWrap = false, richText = false };
            EditorGUILayout.TextArea(report, style);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPlayer()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("플레이어 ( 가정값 )", EditorStyles.boldLabel);
        playerData = (PlayerBaseData)EditorGUILayout.ObjectField("PlayerBaseData", playerData, typeof(PlayerBaseData), false);
        using (new EditorGUILayout.HorizontalScope())
        {
            playerAtk = EditorGUILayout.FloatField("공격력", playerAtk);
            playerHp = EditorGUILayout.FloatField("최대 HP", playerHp);
            playerCd = EditorGUILayout.FloatField("공격 쿨", playerCd);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            hungerDrain = EditorGUILayout.FloatField("허기 감소/초", hungerDrain);
            maxHunger = EditorGUILayout.FloatField("최대 허기", maxHunger);
        }
    }

    private void DrawMonsters()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("일반 적 ( MonsterData )", EditorStyles.boldLabel);
        int atk = Mathf.RoundToInt(playerAtk);
        foreach (MonsterEntry m in monsters)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int hits = atk > 0 ? Mathf.CeilToInt(m.HP / atk) : 0;
                EditorGUILayout.LabelField($"{m.Data.MonsterID} {m.Data.MonsterName}  ( 처치 {hits}타 )", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(m.Users, EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    m.HP = EditorGUILayout.FloatField("HP", m.HP);
                    m.ATK = EditorGUILayout.FloatField("ATK", m.ATK);
                    m.TargetHits = EditorGUILayout.IntField("목표 타수", m.TargetHits);
                }
            }
        }
    }

    private void DrawFountain()
    {
        FloorDef floor = floors.FirstOrDefault(f => f.Type == FloorType.Fountain);
        if (floor == null || floor.Variants.Count == 0) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("4층 샘 정화", EditorStyles.boldLabel);
        foreach (FloorVariant v in floor.Variants)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                float clean = v.Inc > 0f ? 1f / v.Inc : 0f;
                EditorGUILayout.LabelField($"{v.MapName}  ( 스폰 {v.Points}곳 / 적 없이 {clean:0}s 필요 )", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    v.Respawn = EditorGUILayout.FloatField("리스폰 대기", v.Respawn);
                    EditorGUILayout.LabelField($"증가 {v.Inc}/s  감소 {v.Dec}/s", GUILayout.Width(170f));
                }
            }
        }
    }

    private void DrawBoss()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"보스 ( {(bossData != null ? bossData.bossName : "없음")} )", EditorStyles.boldLabel);
        if (bossData == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            bossHp = EditorGUILayout.FloatField("최대 HP", bossHp);
            EditorGUILayout.LabelField($"스턴 시에만 피해 : {bossInvulnerable}", GUILayout.Width(160f));
        }

        foreach (PatternEntry p in patterns)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(p.Name + (p.HasDamageField ? "" : "  ( 피해 없음 )"), EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!p.HasDamageField))
                    {
                        p.Damage = EditorGUILayout.FloatField("피해", p.Damage);
                    }
                    p.Duration = EditorGUILayout.FloatField("지속(추정)", p.Duration);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    p.HitChances = EditorGUILayout.IntField("피격 기회", p.HitChances);
                    p.ExtraTickChance = EditorGUILayout.Slider("추가틱 확률", p.ExtraTickChance, 0f, 1f);
                }
            }
        }
    }

    private void DrawProfiles()
    {
        EditorGUILayout.Space();
        foldProfiles = EditorGUILayout.Foldout(foldProfiles, "플레이어 숙련도 가정", true);
        if (!foldProfiles) return;

        foreach (SkillProfile p in profiles)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                p.Name = EditorGUILayout.TextField("이름", p.Name);
                p.Accuracy = EditorGUILayout.Slider("명중률", p.Accuracy, 0f, 1f);
                p.DodgeEnemy = EditorGUILayout.Slider("적 공격 회피", p.DodgeEnemy, 0f, 1f);
                p.DodgeBoss = EditorGUILayout.Slider("보스 패턴 회피", p.DodgeBoss, 0f, 1f);
                p.ApproachTime = EditorGUILayout.FloatField("적 접근 시간(초)", p.ApproachTime);
                p.BossUptimeIdle = EditorGUILayout.Slider("보스 대기 중 딜 비율", p.BossUptimeIdle, 0f, 1f);
                p.BossUptimePattern = EditorGUILayout.Slider("보스 패턴 중 딜 비율", p.BossUptimePattern, 0f, 1f);
            }
        }
    }

    private void DrawSettings()
    {
        foldSettings = EditorGUILayout.Foldout(foldSettings, "시뮬레이션 설정", true);
        if (!foldSettings) return;

        runs = Mathf.Max(10, EditorGUILayout.IntField("반복 횟수", runs));
        seed = EditorGUILayout.IntField("시드", seed);
        offTargetPressure = EditorGUILayout.Slider("비타겟 적 공격 확률", offTargetPressure, 0f, 1f);
        time2F = EditorGUILayout.FloatField("2층 소요 시간 ( 발판 )", time2F);
        time3FExtra = EditorGUILayout.FloatField("3층 추가 시간 ( 씨앗 )", time3FExtra);
        travel4F = EditorGUILayout.FloatField("4층 적 샘 도착 시간", travel4F);
        stunCycle = EditorGUILayout.FloatField("보스 스턴 주기 ( 무적 보스 )", stunCycle);
        stunDuration = EditorGUILayout.FloatField("보스 스턴 지속", stunDuration);
        maxFloorTime = EditorGUILayout.FloatField("층 시간 제한 ( 막힘 판정 )", maxFloorTime);
    }

    private void DrawTargets()
    {
        foldTargets = EditorGUILayout.Foldout(foldTargets, "추천값 목표", true);
        if (!foldTargets) return;

        targetProfile = EditorGUILayout.Popup("기준 숙련도", Mathf.Clamp(targetProfile, 0, profiles.Length - 1), profiles.Select(p => p.Name).ToArray());
        targetBossEntryHp = EditorGUILayout.Slider("보스 입장 시 남은 HP", targetBossEntryHp, 0.1f, 1f);
        targetBossTime = EditorGUILayout.FloatField("보스전 목표 시간(초)", targetBossTime);
        targetBossClear = EditorGUILayout.Slider("보스 클리어율 목표", targetBossClear, 0.05f, 1f);
        targetFountainTime = EditorGUILayout.FloatField("4층 목표 시간(초)", targetFountainTime);
    }

    #endregion

    #region 데이터 불러오기

    private void LoadFromScene()
    {
        monsters.Clear();
        patterns.Clear();
        floors.Clear();
        loaded = false;

        dungeon = FindAnyObjectByType<DungeonSpring>(FindObjectsInactive.Include);
        if (dungeon == null)
        {
            EditorUtility.DisplayDialog("전투 시뮬레이터", "현재 씬에 DungeonSpring 이 없습니다. 던전 씬을 열어 주세요.", "확인");
            return;
        }

        LoadPlayer();

        //1층 - 슬라임 n 마리
        FloorDef f1 = new FloorDef { Name = "1층", Type = FloorType.Room };
        FloorVariant v1 = new FloorVariant { MapName = "1F" };
        EnemyProto slime = MakeProto(dungeon.SlimePrefab);
        if (slime != null)
        {
            for (int i = 0; i < dungeon.EnemyCount1F; i++) v1.Enemies.Add(slime);
        }
        f1.Variants.Add(v1);
        floors.Add(f1);

        //2층 - 발판 ( 전투 없음 )
        floors.Add(new FloorDef { Name = "2층", Type = FloorType.NoCombat, FixedTime = () => time2F });

        //3층 - 맵마다 적 구성 다름 ( 포탈 랜덤 )
        FloorDef f3 = new FloorDef { Name = "3층", Type = FloorType.Room, FixedTime = () => time3FExtra };
        foreach (DungeonSpring3FMap map in dungeon.Maps3F ?? new DungeonSpring3FMap[0])
        {
            FloorVariant v = new FloorVariant { MapName = map.Map != null ? map.Map.name : "?" };
            foreach (EnemyBase prefab in map.EnemyPrefabs ?? new EnemyBase[0])
            {
                EnemyProto proto = MakeProto(prefab);
                if (proto != null) v.Enemies.Add(proto);
            }
            f3.Variants.Add(v);
        }
        floors.Add(f3);

        //4층 - 샘 정화 + 무한 리스폰
        FloorDef f4 = new FloorDef { Name = "4층", Type = FloorType.Fountain };
        DungeonSpring4FMap[] maps4F = dungeon.Maps4F ?? new DungeonSpring4FMap[0];
        for (int i = 0; i < maps4F.Length; i++)
        {
            DungeonSpring4FMap map = maps4F[i];
            FloorVariant v = new FloorVariant
            {
                Index = i,
                MapName = map.Map != null ? map.Map.name : "?",
                Respawn = map.RespawnDelay,
                Points = map.EnemySpawnPoints != null ? map.EnemySpawnPoints.Length : 0,
                Inc = 0.05f,
                Dec = 0.05f,
            };
            if (map.Fountain != null)
            {
                SerializedObject so = new SerializedObject(map.Fountain);
                v.Inc = GetFloat(so, "increaseSpeed", v.Inc);
                v.Dec = GetFloat(so, "decreaseSpeed", v.Dec);
            }
            foreach (EnemyBase prefab in map.EnemyPrefabs ?? new EnemyBase[0])
            {
                EnemyProto proto = MakeProto(prefab);
                if (proto != null) v.Enemies.Add(proto);
            }
            f4.Variants.Add(v);
        }
        floors.Add(f4);

        //보스
        LoadBoss();
        floors.Add(new FloorDef { Name = "보스", Type = FloorType.Boss });

        loaded = true;
        report = "";
    }

    private void LoadPlayer()
    {
        PlayerContext ctx = FindAnyObjectByType<PlayerContext>(FindObjectsInactive.Include);
        if (ctx != null && ctx.BaseData != null) playerData = ctx.BaseData;

        if (playerData == null)
        {
            string guid = AssetDatabase.FindAssets("t:PlayerBaseData").FirstOrDefault(g => AssetDatabase.GUIDToAssetPath(g).EndsWith("/PlayerBaseData.asset"));
            if (guid != null) playerData = AssetDatabase.LoadAssetAtPath<PlayerBaseData>(AssetDatabase.GUIDToAssetPath(guid));
        }

        if (playerData != null)
        {
            playerAtk = playerData.AttackPower;
            playerHp = playerData.MaxHp;
            playerCd = playerData.AttackCooldown;
        }

        PlayerVitals vitals = FindAnyObjectByType<PlayerVitals>(FindObjectsInactive.Include);
        if (vitals != null)
        {
            SerializedObject so = new SerializedObject(vitals);
            hungerDrain = GetFloat(so, "hungerDrainPerSecond", hungerDrain);
            maxHunger = GetFloat(so, "maxHunger", maxHunger);
            starveDamage = GetFloat(so, "starveDamage", starveDamage);
            starveInterval = GetFloat(so, "starveInterval", starveInterval);
        }
    }

    private EnemyProto MakeProto(EnemyBase prefab)
    {
        if (prefab == null || prefab.MonsterData == null) return null;

        MonsterEntry entry = monsters.FirstOrDefault(m => m.Data == prefab.MonsterData);
        if (entry == null)
        {
            entry = new MonsterEntry
            {
                Data = prefab.MonsterData,
                HP = prefab.MonsterData.HP,
                ATK = prefab.MonsterData.ATK,
                TargetHits = prefab is EnemySkeleton ? 5 : 3,
            };
            monsters.Add(entry);
        }
        if (!entry.Users.Contains(prefab.name))
        {
            entry.Users += (entry.Users.Length > 0 ? ", " : "") + prefab.name;
        }

        EnemyProto proto = new EnemyProto { Name = prefab.name, M = entry };

        //프리팹이 참조하는 MonsterSlimeData / MonsterSkeletonData 에서 쿨타임 읽기
        foreach (MonoBehaviour mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            SerializedProperty it = new SerializedObject(mb).GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.objectReferenceValue is MonsterSlimeData slime)
                {
                    proto.BaseCd = slime.BaseAttackCooldown;
                    proto.SkillCd = slime.SkillAttackCooldown;
                    proto.StunAfterSkill = slime.StunDuration;
                    return proto;
                }
                if (it.objectReferenceValue is MonsterSkeletonData skeleton)
                {
                    proto.BaseCd = skeleton.BaseAttackCooldown;
                    proto.SkillCd = skeleton.SkillAttackCooldown;
                    proto.StunAfterSkill = skeleton.StunDuration;
                    return proto;
                }
            }
        }
        return proto;
    }

    private void LoadBoss()
    {
        bossData = null;
        BossBase boss = FindAnyObjectByType<BossBase>(FindObjectsInactive.Include);
        if (boss == null || boss.Data == null) return;

        bossData = boss.Data;
        bossHp = bossData.maxHp;
        bossInvulnerable = bossData.invulnerableUnlessStunned;
        bossStunMul = bossData.stunDamageMultiplier;
        bossIdle = bossData.idleIntervalRange;
        bossAvoidRepeat = bossData.avoidRepeatPattern;
        hasStunGimmick = FindAnyObjectByType<BossStunGimmick>(FindObjectsInactive.Include) != null;

        foreach (BossPattern pattern in boss.GetComponentsInChildren<BossPattern>(true))
        {
            patterns.Add(MakePattern(pattern));
        }
    }

    /// <summary>
    /// 패턴 코드의 직렬화 수치로 지속 시간 / 피격 기회 추정
    /// </summary>
    private PatternEntry MakePattern(BossPattern pattern)
    {
        SerializedObject so = new SerializedObject(pattern);
        PatternEntry e = new PatternEntry
        {
            Source = pattern,
            Name = pattern.GetType().Name,
            Weight = pattern.Weight,
            Cooldown = GetFloat(so, "cooldown", 0f),
            HasDamageField = so.FindProperty("damage") != null,
            Damage = GetFloat(so, "damage", 0f),
        };

        float blinkPeriod = GetFloat(so, "blinkOnTime", 0.2f) + GetFloat(so, "blinkOffTime", 0.2f);
        int blinkCount = GetInt(so, "blinkCount", 2);
        float tick = GetFloat(so, "tickInterval", 1f);

        switch (e.Name)
        {
            case "DragonPassPattern":
                e.Duration = blinkCount * blinkPeriod + GetFloat(so, "windupDuration", 0.8f) + GetFloat(so, "passDuration", 0.2f) + GetFloat(so, "exitDuration", 0.4f);
                e.HitChances = 1;
                e.ExtraTickChance = GetFloat(so, "passDuration", 0.2f) > tick ? 0.3f : 0f;
                break;
            case "DragonWaterspoutPattern":
                int rings = Mathf.Max(1, GetArraySize(so, "rings"));
                int nextAfter = GetInt(so, "nextStartAfterBlinks", 2);
                e.Duration = (rings - 1) * nextAfter * blinkPeriod + blinkCount * blinkPeriod + GetFloat(so, "eruptDuration", 1.5f);
                e.HitChances = rings;
                e.ExtraTickChance = 0.2f;
                break;
            case "DragonThunderPattern":
                float zone = GetFloat(so, "zoneDuration", 3f);
                e.Duration = GetFloat(so, "darkenDuration", 0.5f) + GetFloat(so, "warningDuration", 1.2f) + GetFloat(so, "sweepDuration", 2f) + zone;
                e.HitChances = 1;
                e.ExtraTickChance = zone > tick ? 0.25f : 0f;
                break;
            case "DragonNatureCallPattern":
                e.Duration = GetArraySize(so, "spawnPoints") * GetFloat(so, "spawnInterval", 0.2f) + GetFloat(so, "afterSpawnDelay", 1f);
                e.HitChances = 0;
                break;
            default:
                e.Duration = 3f;
                e.HitChances = e.HasDamageField ? 1 : 0;
                break;
        }
        return e;
    }

    private static float GetFloat(SerializedObject so, string name, float fallback)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p == null) return fallback;
        if (p.propertyType == SerializedPropertyType.Float) return p.floatValue;
        if (p.propertyType == SerializedPropertyType.Integer) return p.intValue;
        return fallback;
    }

    private static int GetInt(SerializedObject so, string name, int fallback)
    {
        SerializedProperty p = so.FindProperty(name);
        return p != null && p.propertyType == SerializedPropertyType.Integer ? p.intValue : fallback;
    }

    private static int GetArraySize(SerializedObject so, string name)
    {
        SerializedProperty p = so.FindProperty(name);
        return p != null && p.isArray ? p.arraySize : 0;
    }

    #endregion

    #region 시뮬레이션

    private const float Dt = 0.05f;

    private int PlayerDamage => Mathf.RoundToInt(playerAtk);

    private Ctx NewCtx(System.Random rng, SkillProfile profile, float hpRatio = 1f, float hunger = -1f)
    {
        return new Ctx
        {
            Rng = rng,
            P = profile,
            MaxHp = playerHp,
            Hp = playerHp * hpRatio,
            Hunger = hunger < 0f ? maxHunger : hunger,
        };
    }

    /// <summary>
    /// 시간 경과 - 허기 감소 / 굶주림 피해 ( PlayerVitals 와 동일 규칙 )
    /// </summary>
    private void Advance(Ctx c, float dt)
    {
        c.Hunger = Mathf.Max(0f, c.Hunger - hungerDrain * dt);
        if (c.Hunger > 0f) return;

        c.Starved = true;
        c.StarveAcc += dt;
        while (c.StarveAcc >= starveInterval)
        {
            c.StarveAcc -= starveInterval;
            c.Hp -= starveDamage;
        }
    }

    private EnemyRt NewEnemy(Ctx c, EnemyProto p, float delay)
    {
        return new EnemyRt
        {
            P = p,
            Hp = p.M.HP,
            BaseT = delay + c.Range(0.3f, 1f) * p.BaseCd,
            SkillT = delay + c.Range(0.5f, 1f) * p.SkillCd,
        };
    }

    /// <summary>
    /// 적 1마리 행동 - 스킬 우선, 스킬 후 기절
    /// </summary>
    private void EnemyAct(Ctx c, EnemyRt e, float pressure)
    {
        if (e.Stun > 0f)
        {
            e.Stun -= Dt;
            return;
        }

        e.BaseT -= Dt;
        e.SkillT -= Dt;

        if (e.SkillT <= 0f)
        {
            e.SkillT = e.P.SkillCd;
            e.BaseT = Mathf.Max(e.BaseT, 0.5f);
            e.Stun = e.P.StunAfterSkill;
            TryHitPlayer(c, e.P.M.ATK * simEnemyAtkMul, pressure);
        }
        else if (e.BaseT <= 0f)
        {
            e.BaseT = e.P.BaseCd;
            TryHitPlayer(c, e.P.M.ATK * simEnemyAtkMul, pressure);
        }
    }

    private static void TryHitPlayer(Ctx c, float damage, float pressure)
    {
        if (c.R() >= pressure) return;
        if (c.R() < c.P.DodgeEnemy) return;
        c.Hp -= damage;
    }

    /// <summary>
    /// 일반 방 - 모든 적 처치까지
    /// </summary>
    private float SimRoom(Ctx c, List<EnemyProto> list)
    {
        List<EnemyRt> enemies = list.Select(p => NewEnemy(c, p, c.P.ApproachTime)).ToList();
        float t = 0f;
        float nextAtk = c.P.ApproachTime;

        while (enemies.Count > 0 && c.Hp > 0f && t < maxFloorTime)
        {
            t += Dt;
            Advance(c, Dt);

            if (t >= nextAtk)
            {
                nextAtk = t + playerCd;
                if (c.R() < c.P.Accuracy)
                {
                    EnemyRt target = enemies[0];
                    target.Hp -= PlayerDamage;
                    if (target.Hp <= 0f)
                    {
                        enemies.RemoveAt(0);
                        nextAtk = t + c.P.ApproachTime * 0.5f;
                    }
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAct(c, enemies[i], i == 0 ? 1f : offTargetPressure);
            }
        }
        return t;
    }

    /// <summary>
    /// 4층 - 샘 안에 적이 없을 때만 정화 증가, 적이 들어오면 감소. 적은 죽으면 리스폰
    /// </summary>
    private float SimFountain(Ctx c, FloorVariant v, out bool stuck)
    {
        stuck = false;
        if (v.Enemies.Count == 0 || v.Inc <= 0f) return 1f / Mathf.Max(0.0001f, v.Inc);

        List<EnemyRt> enemies = new List<EnemyRt>();
        for (int i = 0; i < v.Points; i++)
        {
            enemies.Add(Spawn4F(c, v, 0f));
        }

        float t = 0f;
        float gauge = 0f;
        float nextAtk = 0f;

        while (c.Hp > 0f && t < maxFloorTime)
        {
            t += Dt;
            Advance(c, Dt);

            EnemyRt target = null;
            bool anyInside = false;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRt e = enemies[i];
                if (e.Dead)
                {
                    if (t >= e.RespawnAt) enemies[i] = Spawn4F(c, v, t);
                    continue;
                }
                if (!e.Arrived && t >= e.ArriveAt) e.Arrived = true;
                if (!e.Arrived) continue;

                anyInside = true;
                if (target == null) target = e;
                EnemyAct(c, e, 1f);
            }

            gauge = anyInside ? Mathf.Max(0f, gauge - v.Dec * Dt) : Mathf.Min(1f, gauge + v.Inc * Dt);
            if (gauge >= 1f) return t;

            if (target != null && t >= nextAtk)
            {
                nextAtk = t + playerCd;
                if (c.R() < c.P.Accuracy)
                {
                    target.Hp -= PlayerDamage;
                    if (target.Hp <= 0f)
                    {
                        target.Dead = true;
                        target.Arrived = false;
                        target.RespawnAt = t + v.Respawn;
                        nextAtk = t + c.P.ApproachTime * 0.5f;
                    }
                }
            }
        }

        stuck = c.Hp > 0f;
        return t;
    }

    private EnemyRt Spawn4F(Ctx c, FloorVariant v, float now)
    {
        EnemyProto proto = v.Enemies[c.Rng.Next(v.Enemies.Count)];
        EnemyRt e = NewEnemy(c, proto, 0f);
        e.ArriveAt = now + travel4F * c.Range(0.8f, 1.2f);
        return e;
    }

    /// <summary>
    /// 보스전 - 대기 / 패턴 반복, 패턴 중 피격 기회마다 회피 판정
    /// </summary>
    private float SimBoss(Ctx c, float startHp, out bool killed, out float dps)
    {
        float hp = startHp;
        float t = 0f;
        float dealt = 0f;
        float nextAtk = c.P.ApproachTime;
        float phaseEnd = c.Range(bossIdle.x, bossIdle.y);
        bool inPattern = false;
        PatternEntry current = null, last = null;
        Dictionary<PatternEntry, float> lastEnd = new Dictionary<PatternEntry, float>();
        List<float> hitTimes = new List<float>();
        float nextStun = stunCycle, stunEnd = -1f;

        while (c.Hp > 0f && hp > 0f && t < maxFloorTime)
        {
            t += Dt;
            Advance(c, Dt);

            bool stunned = t < stunEnd;
            if (bossInvulnerable && !stunned && t >= nextStun)
            {
                stunned = true;
                stunEnd = t + stunDuration;
                nextStun = stunEnd + stunCycle;
                inPattern = false;
                hitTimes.Clear();
                phaseEnd = stunEnd + c.Range(bossIdle.x, bossIdle.y);
            }

            if (!stunned)
            {
                if (t >= phaseEnd)
                {
                    if (inPattern)
                    {
                        lastEnd[current] = t;
                        inPattern = false;
                        phaseEnd = t + c.Range(bossIdle.x, bossIdle.y);
                    }
                    else
                    {
                        current = SelectPattern(c, last, lastEnd, t);
                        if (current != null)
                        {
                            last = current;
                            inPattern = true;
                            phaseEnd = t + Mathf.Max(0.1f, current.Duration);
                            hitTimes.Clear();
                            for (int i = 0; i < current.HitChances; i++)
                            {
                                hitTimes.Add(t + c.Range(0.4f, 1f) * current.Duration);
                            }
                        }
                        else
                        {
                            phaseEnd = t + 0.5f;
                        }
                    }
                }

                if (inPattern)
                {
                    for (int i = hitTimes.Count - 1; i >= 0; i--)
                    {
                        if (t < hitTimes[i]) continue;
                        hitTimes.RemoveAt(i);
                        if (c.R() < c.P.DodgeBoss) continue;

                        float damage = current.Damage * simBossDmgMul;
                        c.Hp -= damage;
                        if (c.R() < current.ExtraTickChance) c.Hp -= damage;
                    }
                }
            }

            if (t >= nextAtk)
            {
                nextAtk = t + playerCd;
                bool canHurt = !bossInvulnerable || stunned;
                float uptime = stunned ? 1f : inPattern ? c.P.BossUptimePattern : c.P.BossUptimeIdle;
                if (canHurt && c.R() < uptime * c.P.Accuracy)
                {
                    int damage = stunned ? Mathf.RoundToInt(PlayerDamage * bossStunMul) : PlayerDamage;
                    hp -= damage;
                    dealt += damage;
                }
            }
        }

        killed = hp <= 0f;
        dps = dealt / Mathf.Max(t, 0.01f);
        return t;
    }

    private PatternEntry SelectPattern(Ctx c, PatternEntry last, Dictionary<PatternEntry, float> lastEnd, float t)
    {
        List<PatternEntry> usable = patterns.Where(p => p.Weight > 0f
            && (!lastEnd.TryGetValue(p, out float end) || t >= end + p.Cooldown)).ToList();
        if (bossAvoidRepeat && usable.Count > 1) usable.Remove(last);
        if (usable.Count == 0) return null;

        float total = usable.Sum(p => p.Weight);
        float roll = c.R() * total;
        foreach (PatternEntry p in usable)
        {
            roll -= p.Weight;
            if (roll <= 0f) return p;
        }
        return usable[usable.Count - 1];
    }

    /// <summary>
    /// 던전 전체 반복 실행 ( HP / 허기는 층 사이에 이어짐 )
    /// </summary>
    private Summary RunAll(SkillProfile profile, int runCount, int runSeed, bool stopBeforeBoss = false)
    {
        Summary s = new Summary(floors.Count) { Runs = runCount };
        System.Random rng = new System.Random(runSeed);

        for (int r = 0; r < runCount; r++)
        {
            Ctx c = NewCtx(rng, profile);
            float total = 0f;
            bool dead = false;

            for (int f = 0; f < floors.Count && !dead; f++)
            {
                FloorDef floor = floors[f];
                if (floor.Type == FloorType.Boss)
                {
                    if (stopBeforeBoss) break;

                    s.ReachedBoss++;
                    s.SumBossEntryHp += Mathf.Max(0f, c.Hp) / c.MaxHp;
                    s.SumBossEntryHunger += c.Hunger;
                }

                s.FloorReached[f]++;
                float hpBefore = c.Hp;
                float time = floor.FixedTime();
                bool stuck = false;

                for (float w = 0f; w < time; w += Dt) Advance(c, Dt);

                switch (floor.Type)
                {
                    case FloorType.Room:
                        if (floor.Variants.Count > 0)
                            time += SimRoom(c, floor.Variants[rng.Next(floor.Variants.Count)].Enemies);
                        break;
                    case FloorType.Fountain:
                        if (floor.Variants.Count > 0)
                            time += SimFountain(c, floor.Variants[rng.Next(floor.Variants.Count)], out stuck);
                        break;
                    case FloorType.Boss:
                        if (bossData != null)
                        {
                            float bossTime = SimBoss(c, bossHp, out bool killed, out _);
                            time += bossTime;
                            s.SumBossTime += bossTime;
                            if (killed) s.BossKills++;
                            else if (c.Hp > 0f) stuck = true;
                        }
                        break;
                }

                total += time;
                s.FloorTime[f] += time;
                s.FloorHpLost[f] += Mathf.Max(0f, hpBefore - Mathf.Max(0f, c.Hp)) / c.MaxHp;

                if (c.Hp <= 0f)
                {
                    s.FloorDeaths[f]++;
                    dead = true;
                }
                else if (stuck)
                {
                    s.Stuck++;
                    dead = true;
                }
            }

            if (!dead && !stopBeforeBoss) s.Clears++;
            if (c.Starved) s.Starved++;
            s.SumTotalTime += total;
        }
        return s;
    }

    #endregion

    #region 리포트

    private string BuildReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== 전투 시뮬레이션 ( {runs}회 / 시드 {seed} ) ===");
        sb.AppendLine($"플레이어 ATK {PlayerDamage} / HP {playerHp} / 공격쿨 {playerCd}s  ( DPS 최대 {PlayerDamage / Mathf.Max(0.01f, playerCd):0.#} )");
        sb.AppendLine();

        Summary[] results = profiles.Select((p, i) => RunAll(p, runs, seed + i)).ToArray();

        sb.AppendLine("[ 요약 ]");
        sb.AppendLine(Pad("숙련도", 8) + Pad("클리어", 8) + Pad("평균시간", 10) + Pad("보스입장HP", 12) + Pad("보스전", 9) + Pad("막힘", 7) + "굶주림");
        foreach ((SkillProfile p, Summary s) in profiles.Zip(results, (p, s) => (p, s)))
        {
            sb.AppendLine(Pad(p.Name, 8)
                + Pad($"{s.ClearRate:P0}", 8)
                + Pad($"{s.SumTotalTime / s.Runs:0}s", 10)
                + Pad($"{s.AvgBossEntryHp:P0}", 12)
                + Pad($"{s.AvgBossTime:0}s", 9)
                + Pad($"{(float)s.Stuck / s.Runs:P0}", 7)
                + $"{(float)s.Starved / s.Runs:P0}");
        }

        sb.AppendLine();
        sb.AppendLine("[ 층별 ( 도달 기준 평균 ) ]");
        for (int i = 0; i < profiles.Length; i++)
        {
            Summary s = results[i];
            sb.AppendLine($"- {profiles[i].Name}");
            for (int f = 0; f < floors.Count; f++)
            {
                int reached = Mathf.Max(1, s.FloorReached[f]);
                sb.AppendLine($"    {Pad(floors[f].Name, 6)} 시간 {s.FloorTime[f] / reached,6:0.0}s   HP 손실 {s.FloorHpLost[f] / reached,5:P0}   사망 {(float)s.FloorDeaths[f] / reached,4:P0}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("[ 진단 ]");
        foreach (string line in Diagnose(results)) sb.AppendLine(line);
        return sb.ToString();
    }

    private List<string> Diagnose(Summary[] results)
    {
        List<string> lines = new List<string>();
        int atk = PlayerDamage;

        foreach (MonsterEntry m in monsters)
        {
            int hits = atk > 0 ? Mathf.CeilToInt(m.HP / atk) : 0;
            if (hits <= 1) lines.Add($"⚠ {m.Data.MonsterName}({m.Data.MonsterID}) : 1타 처치 ( HP {m.HP} ≤ 플레이어 ATK {atk} ) - 전투 의미 없음");
            if (m.ATK / playerHp < 0.03f) lines.Add($"⚠ {m.Data.MonsterName}({m.Data.MonsterID}) : 1회 피해 {m.ATK} = 최대 HP 의 {m.ATK / playerHp:P0} - 위협 낮음");
        }

        if (bossData != null)
        {
            if (!bossInvulnerable && hasStunGimmick)
                lines.Add("⚠ 보스 invulnerableUnlessStunned = false - 스턴 기믹 없이 딜 가능 ( 기믹이 선택 사항이 됨 )");
            if (patterns.All(p => !p.HasDamageField || p.Damage <= 0f))
                lines.Add("⚠ 보스 패턴 피해가 모두 0");
        }

        Summary target = results[Mathf.Clamp(targetProfile, 0, results.Length - 1)];
        string name = profiles[Mathf.Clamp(targetProfile, 0, profiles.Length - 1)].Name;

        for (int f = 0; f < floors.Count; f++)
        {
            if (floors[f].Type == FloorType.NoCombat || floors[f].Type == FloorType.Boss) continue;
            int reached = Mathf.Max(1, target.FloorReached[f]);
            float lost = target.FloorHpLost[f] / reached;
            if (lost < 0.03f) lines.Add($"⚠ [{name}] {floors[f].Name} HP 손실 {lost:P0} - 너무 쉬움");
            if (lost > 0.4f) lines.Add($"⚠ [{name}] {floors[f].Name} HP 손실 {lost:P0} - 한 층에서 과도한 피해");
        }

        for (int f = 0; f < floors.Count; f++)
        {
            if (floors[f].Type != FloorType.Fountain || target.FloorReached[f] == 0) continue;
            float time = target.FloorTime[f] / target.FloorReached[f];
            if (time > targetFountainTime * 2f)
                lines.Add($"⚠ [{name}] {floors[f].Name} 평균 {time:0}s ( 목표 {targetFountainTime:0}s ) - 리스폰이 정화를 계속 끊음. 리스폰 대기 / 정화 속도 조정 필요 ( 추천값 계산 시 리스폰 대기 제안 )");
        }

        for (int i = 0; i < results.Length; i++)
        {
            float stuck = (float)results[i].Stuck / results[i].Runs;
            if (stuck > 0.05f) lines.Add($"⚠ [{profiles[i].Name}] 시간 제한 내 진행 불가 {stuck:P0} ( 4층 정화 / 보스 딜 부족 확인 )");
            float starved = (float)results[i].Starved / results[i].Runs;
            if (starved > 0.1f) lines.Add($"⚠ [{profiles[i].Name}] 굶주림 발생 {starved:P0} ( 평균 보스 입장 허기 {results[i].AvgBossEntryHunger:0} )");
        }

        if (target.ReachedBoss > 0)
        {
            if (target.AvgBossTime < 30f) lines.Add($"⚠ [{name}] 보스전 {target.AvgBossTime:0}s - 너무 짧음 ( 목표 {targetBossTime:0}s )");
            if (target.AvgBossTime > 180f) lines.Add($"⚠ [{name}] 보스전 {target.AvgBossTime:0}s - 너무 김");
        }

        if (target.ClearRate > 0.95f) lines.Add($"⚠ [{name}] 클리어율 {target.ClearRate:P0} - 쉬움");
        if (target.ClearRate < 0.6f) lines.Add($"⚠ [{name}] 클리어율 {target.ClearRate:P0} - 어려움");
        if (results.Length >= 3 && results[0].ClearRate > 0.9f) lines.Add($"⚠ [{profiles[0].Name}] 도 {results[0].ClearRate:P0} 클리어 - 숙련도 차이가 결과에 반영 안 됨");

        if (lines.Count == 0) lines.Add("문제 없음 ( 목표 범위 안 )");
        return lines;
    }

    private static string Pad(string text, int width)
    {
        //한글은 2칸으로 계산
        int len = text.Sum(ch => ch > 0x1100 ? 2 : 1);
        return text + new string(' ', Mathf.Max(1, width - len));
    }

    #endregion

    #region 추천값

    /// <summary>
    /// 목표 기준 추천값 계산 후 표에 채움 ( 에셋은 변경 X )
    /// 1. 적 HP = 플레이어 ATK x 목표 타수
    /// 2. 적 ATK = 보스 입장 HP 가 목표가 되도록 배율 이분 탐색
    /// 3. 보스 HP = 평균 DPS x 목표 보스전 시간
    /// 4. 보스 패턴 피해 = 목표 입장 HP 로 시작했을 때 클리어율이 목표가 되도록 배율 이분 탐색
    /// </summary>
    private void Recommend()
    {
        SkillProfile profile = profiles[Mathf.Clamp(targetProfile, 0, profiles.Length - 1)];
        int sampleRuns = Mathf.Clamp(runs / 2, 100, 400);

        try
        {
            EditorUtility.DisplayProgressBar("전투 시뮬레이터", "적 HP 계산", 0.1f);
            foreach (MonsterEntry m in monsters)
            {
                m.HP = Mathf.Max(1, PlayerDamage * Mathf.Max(1, m.TargetHits));
            }

            EditorUtility.DisplayProgressBar("전투 시뮬레이터", "4층 리스폰 계산", 0.2f);
            foreach (FloorDef floor in floors.Where(f => f.Type == FloorType.Fountain))
            {
                foreach (FloorVariant v in floor.Variants)
                {
                    float respawn = Bisect(r =>
                    {
                        v.Respawn = r;
                        return AvgFountainTime(profile, v, sampleRuns / 2);
                    }, targetFountainTime, 0.5f, 60f);
                    v.Respawn = Mathf.Round(respawn * 2f) / 2f;
                }
            }

            EditorUtility.DisplayProgressBar("전투 시뮬레이터", "적 ATK 계산", 0.3f);
            float atkMul = Bisect(k =>
            {
                simEnemyAtkMul = k;
                Summary s = RunAll(profile, sampleRuns, seed, true);
                float sum = 0f;
                for (int f = 0; f < floors.Count; f++) sum += s.FloorHpLost[f];
                return 1f - sum / s.Runs; //보스 입장 HP 비율 ( 사망 포함 )
            }, targetBossEntryHp, 0.05f, 50f);
            simEnemyAtkMul = 1f;
            foreach (MonsterEntry m in monsters)
            {
                m.ATK = Mathf.Max(0.5f, Mathf.Round(m.ATK * atkMul * 2f) / 2f);
            }

            if (bossData != null)
            {
                EditorUtility.DisplayProgressBar("전투 시뮬레이터", "보스 HP 계산", 0.6f);
                System.Random rng = new System.Random(seed);
                float dpsSum = 0f;
                const int dpsRuns = 100;
                float savedMax = maxFloorTime;
                maxFloorTime = 120f;
                for (int i = 0; i < dpsRuns; i++)
                {
                    Ctx c = NewCtx(rng, profile);
                    c.Hp = float.MaxValue;
                    c.Hunger = float.MaxValue;
                    SimBoss(c, float.MaxValue, out _, out float dps);
                    dpsSum += dps;
                }
                maxFloorTime = savedMax;
                bossHp = Mathf.Max(10f, Mathf.Round(dpsSum / dpsRuns * targetBossTime / 10f) * 10f);

                EditorUtility.DisplayProgressBar("전투 시뮬레이터", "보스 패턴 피해 계산", 0.8f);
                float entryHunger = Mathf.Max(0f, maxHunger - hungerDrain * EstimateTimeBeforeBoss(profile, sampleRuns));
                float dmgMul = Bisect(m =>
                {
                    simBossDmgMul = m;
                    System.Random r = new System.Random(seed);
                    int kills = 0;
                    for (int i = 0; i < sampleRuns; i++)
                    {
                        Ctx c = NewCtx(r, profile, targetBossEntryHp, entryHunger);
                        SimBoss(c, bossHp, out bool killed, out _);
                        if (killed) kills++;
                    }
                    return (float)kills / sampleRuns;
                }, targetBossClear, 0.05f, 30f);
                simBossDmgMul = 1f;
                foreach (PatternEntry p in patterns.Where(p => p.HasDamageField))
                {
                    p.Damage = Mathf.Max(1f, Mathf.Round(p.Damage * dmgMul));
                }
            }
        }
        finally
        {
            simEnemyAtkMul = 1f;
            simBossDmgMul = 1f;
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 4층 정화 평균 시간 ( 사망 / 허기 무시 - 순수 정화 속도만 측정 )
    /// </summary>
    private float AvgFountainTime(SkillProfile profile, FloorVariant v, int count)
    {
        System.Random rng = new System.Random(seed);
        float sum = 0f;
        for (int i = 0; i < count; i++)
        {
            Ctx c = NewCtx(rng, profile);
            c.Hp = float.MaxValue;
            c.Hunger = float.MaxValue;
            sum += SimFountain(c, v, out _);
        }
        return sum / Mathf.Max(1, count);
    }

    private float EstimateTimeBeforeBoss(SkillProfile profile, int sampleRuns)
    {
        Summary s = RunAll(profile, sampleRuns, seed, true);
        float sum = 0f;
        for (int f = 0; f < floors.Count; f++)
        {
            if (s.FloorReached[f] > 0) sum += s.FloorTime[f] / s.FloorReached[f];
        }
        return sum;
    }

    /// <summary>
    /// 감소 함수 f 에서 f(x) = target 이 되는 x 탐색 ( 로그 스케일 )
    /// </summary>
    private static float Bisect(Func<float, float> f, float target, float lo, float hi)
    {
        if (f(lo) <= target) return lo;
        if (f(hi) >= target) return hi;

        for (int i = 0; i < 14; i++)
        {
            float mid = Mathf.Sqrt(lo * hi);
            if (f(mid) > target) lo = mid;
            else hi = mid;
        }
        return Mathf.Sqrt(lo * hi);
    }

    #endregion

    #region 적용

    private void ApplyToAssets()
    {
        StringBuilder sb = new StringBuilder();
        foreach (MonsterEntry m in monsters)
        {
            if (m.Data.HP != Mathf.RoundToInt(m.HP) || !Mathf.Approximately(m.Data.ATK, m.ATK))
                sb.AppendLine($"{m.Data.MonsterName}({m.Data.MonsterID}) HP {m.Data.HP} -> {Mathf.RoundToInt(m.HP)}, ATK {m.Data.ATK} -> {m.ATK}");
        }
        foreach (FloorVariant v in dungeon != null ? FountainVariants() : Enumerable.Empty<FloorVariant>())
        {
            SerializedProperty prop = RespawnProperty(new SerializedObject(dungeon), v);
            if (prop != null && !Mathf.Approximately(prop.floatValue, v.Respawn))
                sb.AppendLine($"{v.MapName} RespawnDelay {prop.floatValue} -> {v.Respawn}");
        }
        if (bossData != null && bossData.maxHp != Mathf.RoundToInt(bossHp))
            sb.AppendLine($"보스 maxHp {bossData.maxHp} -> {Mathf.RoundToInt(bossHp)}");
        foreach (PatternEntry p in patterns.Where(p => p.HasDamageField && p.Source != null))
        {
            float current = GetFloat(new SerializedObject(p.Source), "damage", p.Damage);
            if (!Mathf.Approximately(current, p.Damage)) sb.AppendLine($"{p.Name} damage {current} -> {p.Damage}");
        }

        if (sb.Length == 0)
        {
            EditorUtility.DisplayDialog("전투 시뮬레이터", "변경된 값이 없습니다.", "확인");
            return;
        }
        if (!EditorUtility.DisplayDialog("SO 에 적용", "아래 값을 실제 데이터에 적용합니다. ( Undo 가능 )\n\n" + sb, "적용", "취소"))
        {
            return;
        }

        foreach (MonsterEntry m in monsters)
        {
            Undo.RecordObject(m.Data, "전투 시뮬레이터 적용");
            m.Data.HP = Mathf.RoundToInt(m.HP);
            m.Data.ATK = m.ATK;
            EditorUtility.SetDirty(m.Data);
        }

        if (bossData != null)
        {
            Undo.RecordObject(bossData, "전투 시뮬레이터 적용");
            bossData.maxHp = Mathf.Max(1, Mathf.RoundToInt(bossHp));
            EditorUtility.SetDirty(bossData);
        }

        foreach (PatternEntry p in patterns.Where(p => p.HasDamageField && p.Source != null))
        {
            SerializedObject so = new SerializedObject(p.Source);
            so.FindProperty("damage").floatValue = p.Damage;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(p.Source.gameObject.scene);
        }

        if (dungeon != null)
        {
            SerializedObject dso = new SerializedObject(dungeon);
            foreach (FloorVariant v in FountainVariants())
            {
                SerializedProperty prop = RespawnProperty(dso, v);
                if (prop != null) prop.floatValue = v.Respawn;
            }
            dso.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(dungeon.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[전투 시뮬레이터] 적용 완료\n" + sb);
    }

    private IEnumerable<FloorVariant> FountainVariants()
    {
        return floors.Where(f => f.Type == FloorType.Fountain).SelectMany(f => f.Variants).Where(v => v.Index >= 0);
    }

    private static SerializedProperty RespawnProperty(SerializedObject so, FloorVariant v)
    {
        SerializedProperty maps = so.FindProperty("maps4F");
        if (maps == null || v.Index >= maps.arraySize) return null;
        return maps.GetArrayElementAtIndex(v.Index).FindPropertyRelative("RespawnDelay");
    }

    #endregion
}
