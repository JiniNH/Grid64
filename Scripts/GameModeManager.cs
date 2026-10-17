using UnityEngine;

public enum GameMode
{
    Classic,    // 시간 제한 없음, 데드락 시 종료
    TimeAttack, // 제한 시간 내 최고 점수
    Obstacle    // 클래식 + 돌덩이 장애물 (반드시 맨 뒤에 추가)
}

public static class GameModeManager
{
    public static GameMode Current = GameMode.Classic;

    private const string KeyClassic = "BestScore_Classic";
    private const string KeyTimeAttack = "BestScore_TimeAttack";
    private const string KeyObstacle = "BestScore_Obstacle";

    public static string GetKey(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.TimeAttack: return KeyTimeAttack;
            case GameMode.Obstacle:   return KeyObstacle;
            default:                  return KeyClassic;
        }
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

    public static string GetDisplayName(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.TimeAttack: return Localization.Get("mode_timeattack");
            case GameMode.Obstacle:   return Localization.Get("mode_obstacle");
            default:                  return Localization.Get("mode_classic");
        }
    }
}