using UnityEngine;
using System;

public enum SoundChannel { Bgm, Sfx }

// 배경음·효과음 음량 설정 (씬 전환과 무관하게 유지, PlayerPrefs에 저장)
public static class SoundSettings
{
    private const string KeyBgmVolume = "Sound_BgmVolume";
    private const string KeySfxVolume = "Sound_SfxVolume";
    private const string KeyBgmMuted  = "Sound_BgmMuted";
    private const string KeySfxMuted  = "Sound_SfxMuted";

    private static float bgmVolume = 1f;
    private static float sfxVolume = 1f;
    private static bool bgmMuted;
    private static bool sfxMuted;
    private static bool loaded;

    // 최종 배율 (음소거면 0). 오디오 스레드에서도 읽을 수 있도록 미리 계산해 둠
    public static float BgmGain { get; private set; } = 1f;
    public static float SfxGain { get; private set; } = 1f;

    // 값이 바뀌면 알림 (BGMPlayer가 구독)
    public static event Action Changed;

    // 메인 스레드에서 호출할 것 (PlayerPrefs는 메인 스레드 전용)
    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        bgmVolume = PlayerPrefs.GetFloat(KeyBgmVolume, 1f);
        sfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 1f);
        bgmMuted  = PlayerPrefs.GetInt(KeyBgmMuted, 0) == 1;
        sfxMuted  = PlayerPrefs.GetInt(KeySfxMuted, 0) == 1;

        Recalculate();
    }

    public static float GetVolume(SoundChannel ch) => ch == SoundChannel.Bgm ? bgmVolume : sfxVolume;
    public static bool IsMuted(SoundChannel ch)    => ch == SoundChannel.Bgm ? bgmMuted : sfxMuted;

    // 슬라이더 조절 — 0보다 크면 음소거 자동 해제
    public static void SetVolume(SoundChannel ch, float value)
    {
        Load();
        value = Mathf.Clamp01(value);

        if (ch == SoundChannel.Bgm)
        {
            bgmVolume = value;
            if (value > 0.001f) bgmMuted = false;
        }
        else
        {
            sfxVolume = value;
            if (value > 0.001f) sfxMuted = false;
        }

        Store();
        Recalculate();
        Changed?.Invoke();
    }

    // 아이콘 탭 — 볼륨 값은 유지한 채 음소거만 전환
    public static void SetMuted(SoundChannel ch, bool muted)
    {
        Load();
        if (ch == SoundChannel.Bgm) bgmMuted = muted;
        else sfxMuted = muted;

        Store();
        Recalculate();
        Changed?.Invoke();
    }

    // 디스크에 기록 (패널을 닫을 때, 앱이 백그라운드로 갈 때 호출)
    public static void Flush()
    {
        if (loaded) PlayerPrefs.Save();
    }

    // 메모리에만 반영 (슬라이더를 끄는 동안 매 프레임 디스크에 쓰지 않도록)
    private static void Store()
    {
        PlayerPrefs.SetFloat(KeyBgmVolume, bgmVolume);
        PlayerPrefs.SetFloat(KeySfxVolume, sfxVolume);
        PlayerPrefs.SetInt(KeyBgmMuted, bgmMuted ? 1 : 0);
        PlayerPrefs.SetInt(KeySfxMuted, sfxMuted ? 1 : 0);
    }

    // 사람 귀는 소리를 로그 단위로 느끼므로 제곱 곡선을 적용
    // (슬라이더 절반 위치가 "절반 크기"로 들리게 됨)
    private static void Recalculate()
    {
        BgmGain = bgmMuted ? 0f : bgmVolume * bgmVolume;
        SfxGain = sfxMuted ? 0f : sfxVolume * sfxVolume;
    }
}