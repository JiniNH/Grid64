using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    public static BGMPlayer Instance;

    [Header("BGM 설정")]
    public AudioClip bgmClip;         // 재생할 음원
    [Range(0f, 1f)]
    public float volume = 0.35f;      // 볼륨(효과음보다 작게)

    private AudioSource source;

    void Awake()
    {
        // 씬이 리로드되더라도 BGM이 끊기지 않도록 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);     // 중복 생성 방지
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (bgmClip == null) return;

        source.clip = bgmClip;
        source.loop = true;         // 무한 반복
        source.volume = volume;
        source.playOnAwake = false;
        source.Play();
    }

    // 볼륨 주절 (설정 메뉴용) - 미구현
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (source != null) source.volume = volume;
    }
}