using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM/효과음 클립과 씬별 BGM 매핑을 보관하는 데이터
/// </summary>
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Sound/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    [Serializable]
    public class BGMEntry
    {
        public BGMType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public class SFXEntry
    {
        public SFXType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("여러 개 넣으면 clip 대신 이 중 랜덤 재생 (직전 클립 제외)")]
        public AudioClip[] randomClips;
        [Tooltip("피치 랜덤 폭. 0.1이면 0.9~1.1")]
        [Range(0f, 0.5f)] public float pitchRandom = 0f;

        [NonSerialized] private int lastIndex = -1;

        public AudioClip GetClip()
        {
            if (randomClips == null || randomClips.Length == 0) return clip;
            if (randomClips.Length == 1) return randomClips[0];

            // 직전 인덱스를 제외하고 뽑기
            int index = UnityEngine.Random.Range(0, randomClips.Length - 1);
            if (index >= lastIndex && lastIndex >= 0) index++;
            lastIndex = index;
            return randomClips[index];
        }
    }

    [Serializable]
    public class SceneBGMEntry
    {
        public string sceneName;
        public BGMType bgm;
    }

    [SerializeField] private List<BGMEntry> bgmList = new List<BGMEntry>();
    [SerializeField] private List<SFXEntry> sfxList = new List<SFXEntry>();

    [Tooltip("씬 로드 시 자동으로 재생할 BGM. 목록에 없는 씬은 기존 BGM 유지, None이면 정지")]
    [SerializeField] private List<SceneBGMEntry> sceneBGMList = new List<SceneBGMEntry>();

    private Dictionary<BGMType, BGMEntry> bgmDict;
    private Dictionary<SFXType, SFXEntry> sfxDict;
    private Dictionary<string, BGMType> sceneBGMDict;

    /// <summary>
    /// 리스트를 딕셔너리로 변환 (SoundManager Awake에서 호출)
    /// </summary>
    public void Init()
    {
        bgmDict = new Dictionary<BGMType, BGMEntry>();
        foreach (BGMEntry entry in bgmList)
        {
            if (entry.type == BGMType.None) continue;
            bgmDict[entry.type] = entry;
        }

        sfxDict = new Dictionary<SFXType, SFXEntry>();
        foreach (SFXEntry entry in sfxList)
        {
            if (entry.type == SFXType.None) continue;
            sfxDict[entry.type] = entry;
        }

        sceneBGMDict = new Dictionary<string, BGMType>();
        foreach (SceneBGMEntry entry in sceneBGMList)
        {
            if (string.IsNullOrEmpty(entry.sceneName)) continue;
            sceneBGMDict[entry.sceneName] = entry.bgm;
        }
    }

    public BGMEntry GetBGM(BGMType type)
    {
        if (bgmDict != null && bgmDict.TryGetValue(type, out BGMEntry entry))
        {
            return entry;
        }
        return null;
    }

    public SFXEntry GetSFX(SFXType type)
    {
        if (sfxDict != null && sfxDict.TryGetValue(type, out SFXEntry entry))
        {
            return entry;
        }
        return null;
    }

    /// <summary>
    /// 씬에 매핑된 BGM 조회. 매핑 없으면 false
    /// </summary>
    public bool TryGetSceneBGM(string sceneName, out BGMType bgm)
    {
        bgm = BGMType.None;
        return sceneBGMDict != null && sceneBGMDict.TryGetValue(sceneName, out bgm);
    }
}
