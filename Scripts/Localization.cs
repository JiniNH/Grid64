using UnityEngine;
using System;
using System.Collections.Generic;

public enum Language { Korean, English }

// 문구 표와 현재 언어 (씬 전환과 무관하게 유지, PlayerPrefs에 저장)
public static class Localization
{
    private const string PrefKey = "Language";
    private static bool loaded;

    public static Language Current { get; private set; } = Language.Korean;

    // 언어가 바뀌면 알림 (LocalizedText, 메인 메뉴가 구독)
    public static event Action Changed;

    // 키 → { 한국어, English }
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        // ===== 메인 메뉴 =====
        { "mode_classic",      new[] { "클  래  식",              "Classic" } },
        { "mode_timeattack",   new[] { "타 임 어 택",            "Time Attack" } },
        { "best",              new[] { "최고 기록",           "Best" } },

        // ===== 설정 =====
        { "settings_title",    new[] { "설 정",                "Settings" } },
        { "bgm",               new[] { "배경음",              "Music" } },
        { "sfx",               new[] { "효과음",              "Sound" } },
        { "language",          new[] { "언어",                "Language" } },
        { "privacy",           new[] { "개인정보처리방침",     "Privacy Policy" } },
        { "licenses",          new[] { "오픈소스 라이선스",    "Licenses" } },
        { "close",             new[] { "닫기",                "Close" } },

        // ===== 일시정지 =====
        { "pause_title",       new[] { "일 시 정 지",            "Paused" } },
        { "resume",            new[] { "계 속 하 기 ",            "Resume" } },
        { "restart",           new[] { "다 시 시 작",           "Restart" } },
        { "main_menu",         new[] { "메 인 메 뉴",           "Main Menu" } },

        // ===== 게임오버 =====
        { "game_over",         new[] { "게임 오버",           "Game Over" } },
        { "new_record",        new[] { "신기록!",             "New Record!" } },
        { "score",             new[] { "점수",                "Score" } },
        { "share_record",      new[] { "기록 공유",            "Share Score" } },
        { "share_title",       new[] { "공유하기",             "Share via" } },
        { "share_message",     new[] { "Grid64 {0}에서 {1}점 달성! 🏆 도전해보세요 👉 {2}",
                                       "I scored {1} in Grid64 {0}! 🏆 Can you beat it? 👉 {2}" } },
        { "share_message_normal", new[] { "Grid64 {0}에서 {1}점! 나보다 잘할 수 있어? 👉 {2}",
                                          "I scored {1} in Grid64 {0}! Can you do better? 👉 {2}" } },

        // ===== 인게임 연출 =====
        { "combo",             new[] { "{0} 콤보!",           "{0} Combo!" } },
        { "all_clear",         new[] { "올 클리어!",          "All Clear!" } },
        { "fever_time",        new[] { "피버 타임!",          "Fever Time!" } },
        { "time_bonus",        new[] { "+{0:0}초",            "+{0:0}s" } },
        { "time_penalty",      new[] { "-{0:0}초",            "-{0:0}s" } },
        { "speed_up",          new[] { "스피드 업!",          "SPEED UP!" } },

        // ===== 부활 =====
        { "revive_title",      new[] { "이어서 할까요?",       "Continue?" } },
        { "revive_score",      new[] { "현재 점수",           "Current Score" } },
        { "revive_reward",     new[] { "  블록 3줄 비우기",    "  Clear 3 Lines" } },
        { "revive_watch",      new[] { "광고 보고 이어하기",   "Watch Ad to Continue" } },
        { "revive_decline",    new[] { "괜찮아요",            "No Thanks" } },
    };

    // 메인 스레드에서 호출할 것 (PlayerPrefs는 메인 스레드 전용)
    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        if (PlayerPrefs.HasKey(PrefKey))
        {
            int saved = PlayerPrefs.GetInt(PrefKey);
            Current = saved == (int)Language.English ? Language.English : Language.Korean;
        }
        else
        {
            // 처음 실행: 기기 언어가 한국어면 한국어, 그 외는 영어
            Current = Application.systemLanguage == SystemLanguage.Korean
                ? Language.Korean
                : Language.English;
        }
    }

    public static void Set(Language lang)
    {
        Load();
        if (Current == lang) return;

        Current = lang;
        PlayerPrefs.SetInt(PrefKey, (int)lang);
        PlayerPrefs.Save();

        Changed?.Invoke();
    }

    public static string Get(string key)
    {
        Load();

        if (Table.TryGetValue(key, out string[] values))
            return values[(int)Current];

        Debug.LogWarning($"[Localization] 표에 없는 키: {key}");
        return key;   // 키가 그대로 보이면 표에 추가해야 한다는 신호
    }

    // "{0} 콤보!"처럼 숫자가 들어가는 문구용
    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}