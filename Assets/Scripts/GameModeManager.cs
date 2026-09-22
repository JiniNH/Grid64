using UnityEngine;

public enum GameMode
{
    Classic,    // 시간 제한 없음, 데드락 시 종료
    TimeAttack  // 제한 시간 내 최고 점수
}

public static class GameModeManager
{
    // 현재 선택된 모드 (메인 메뉴에서 설정, 인게임에서 읽음)
    public static GameMode Current = GameMode.Classic;

    private const string KeyClassic = "BestScore_Classic";
    private const string KeyTimeAttack = "BestScore_TimeAttack";

    // 모드별 PlayerPrefs 키
    public static string GetKey(GameMode mode)
    {
        return mode == GameMode.TimeAttack ? KeyTimeAttack : KeyClassic;
    }

    public static int GetBestScore(GameMode mode)
    {
        return PlayerPrefs.GetInt(GetKey(mode), 0);
    }

    public static void SetBestScore(GameMode mode, int score)
    {
        PlayerPrefs.SetInt(GetKey(mode), score);
        PlayerPrefs.Save();
    }

    // 모드 표시용 이름
    public static string GetDisplayName(GameMode mode)
    {
        return mode == GameMode.TimeAttack ? "TIME ATTACK" : "CLASSIC";
    }
}