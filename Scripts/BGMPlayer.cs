using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    public static BGMPlayer Instance;

    [Header("BGM 설정")]
    public AudioClip bgmClip;         // 재생할 음원
    [Range(0f, 1f)]
    public float volume = 0.35f;      // 기본 볼륨 (설정 슬라이더 값이 여기에 곱해짐)

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

        SoundSettings.Load();
        SoundSettings.Changed += ApplyVolume;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SoundSettings.Changed -= ApplyVolume;
            Instance = null;
        }
    }

    void Start()
    {
        if (bgmClip == null) return;

        source.clip = bgmClip;
        source.loop = true;
        source.playOnAwake = false;
        ApplyVolume();
        source.Play();
    }

    // 최종 음량 = 기본 볼륨 × 설정 음량 (음소거면 0)
    private void ApplyVolume()
    {
        if (source != null) source.volume = volume * SoundSettings.BgmGain;
    }

    // 기본 볼륨 조절 (코드에서 쓸 때)
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        ApplyVolume();
    }

    // 재생 속도/음높이 조절 (피버 연출용)
    public void SetPitch(float target, float duration)
    {
        StopCoroutine(nameof(PitchRoutine));

        if (duration <= 0f)
        {
            if (source != null) source.pitch = target;
            return;
        }

        StartCoroutine(PitchRoutine(target, duration));
    }

    private IEnumerator PitchRoutine(float target, float duration)
    {
        if (source == null) yield break;

        float start = source.pitch;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.pitch = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        source.pitch = target;
    }
}