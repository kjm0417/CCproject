using UnityEngine;

/// <summary>
/// 보스 조우 BGM - 보스 오브젝트에 부착
/// 보스 맵 진입 ( 보스 활성화 ) 시 보스 BGM 재생, 보스 사망 / 맵 이탈 시 이전 BGM 복귀
/// </summary>
[RequireComponent(typeof(BossBase))]
public class BossBGMPlayer : MonoBehaviour
{
    [SerializeField, Tooltip("조우 시 재생할 BGM")]
    private BGMType bossBGM = BGMType.Boss;

    private BossBase boss;
    private BGMType prevBGM = BGMType.None;
    private bool isPlaying;
    private bool started;

    private void Awake()
    {
        boss = GetComponent<BossBase>();
    }

    private void OnEnable()
    {
        boss.OnDiedEvent += RestoreBGM;

        // 첫 활성화는 Start 에서 처리 ( 씬 로드 직후면 sceneLoaded 의 씬 BGM 에 덮이므로 )
        if (started) PlayBossBGM();
    }

    private void Start()
    {
        started = true;
        PlayBossBGM();
    }

    private void OnDisable()
    {
        boss.OnDiedEvent -= RestoreBGM;

        //보스 처치 전 맵 이탈
        RestoreBGM();
    }

    private void PlayBossBGM()
    {
        if (isPlaying || boss.IsDead) return;

        SoundManager sound = SoundManager.Instance;
        if (sound == null) return;

        prevBGM = sound.CurrentBGM;
        sound.PlayBGM(bossBGM);
        isPlaying = true;
    }

    private void RestoreBGM()
    {
        if (!isPlaying) return;
        isPlaying = false;

        SoundManager sound = SoundManager.Instance;
        if (sound == null) return;

        sound.PlayBGM(prevBGM);
    }
}
