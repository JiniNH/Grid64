using UnityEngine;
using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

// Google Play 게임즈 리더보드 — 자동 로그인, 점수 등록, 랭킹 화면
// 씬에 배치하지 않아도 게임이 시작되면 스스로 초기화됨 (InAppReview와 같은 방식)
public static class RankingManager
{
    public static bool IsSignedIn { get; private set; }

    // 모드별로 "서버에 올라간 것이 확인된 최고 점수" — 오프라인·미로그인 때의 기록을 나중에 올리기 위함
    private const string KeySyncedPrefix = "Rank_SyncedBest_";

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool isSigningIn;
#endif

    // ===== 초기화: 앱 시작 시 자동 로그인 =====
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        PlayGamesPlatform.DebugLogEnabled = Debug.isDebugBuild;
        PlayGamesPlatform.Activate();
        isSigningIn = true;
        PlayGamesPlatform.Instance.Authenticate(OnAuthResult);   // 조용한 자동 로그인
#else
        Debug.Log("[Rank] Editor: Play Games disabled");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void OnAuthResult(SignInStatus status)
    {
        isSigningIn = false;
        IsSignedIn = status == SignInStatus.Success;
        Debug.Log("[Rank] Sign-in: " + status);

        if (IsSignedIn) SyncBestScores();
    }
#endif

    // ===== 리더보드 ID =====
    // GPGSIds.cs는 Window > Google Play Games > Setup > Android setup에서 자동 생성됨
    private static string GetLeaderboardId(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.TimeAttack: return GPGSIds.leaderboard_time_attack;
            case GameMode.Obstacle:   return GPGSIds.leaderboard_obstacle;
            default:                  return GPGSIds.leaderboard_classic;
        }
    }

    // ===== 점수 등록: BoardManager.ShowGameOver()에서 호출 =====
    // 매 판 점수를 보냄 (일간·주간 순위에도 반영되도록). 최고 기록은 Google이 알아서 유지
    // onRank: 등록 후 내 전체 순위(1부터)를 받음. 모르면 0 (로그인 안 됨·비공개 프로필·네트워크 오류)
    //         onRank를 넘기면 어떤 경우든 반드시 한 번 호출됨
    public static void SubmitScore(GameMode mode, int score, Action<int> onRank = null)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsSignedIn)   // 로그인 전 기록은 다음 로그인 때 SyncBestScores가 올림
        {
            onRank?.Invoke(0);
            return;
        }

        if (score <= 0)    // 0점은 등록할 필요 없음 — 기존 순위만 조회
        {
            if (onRank != null) FetchRank(mode, onRank);
            return;
        }

        string id = GetLeaderboardId(mode);
        PlayGamesPlatform.Instance.ReportScore(score, id, ok =>
        {
            Debug.Log("[Rank] Report " + mode + " " + score + ": " + (ok ? "OK" : "FAIL"));
            if (ok && score > GetSynced(mode)) SetSynced(mode, score);
            if (onRank != null) FetchRank(mode, onRank);   // 등록이 끝난 뒤 순위 조회
        });
#else
        Debug.Log("[Rank] Editor: SubmitScore " + mode + " " + score);
        onRank?.Invoke(0);
#endif
    }

    // 내 전체 기간 순위 (공개 리더보드 기준). 모르면 0
    public static void FetchRank(GameMode mode, Action<int> onRank)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsSignedIn) { onRank(0); return; }

        PlayGamesPlatform.Instance.LoadScores(
            GetLeaderboardId(mode),
            LeaderboardStart.PlayerCentered,
            1,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            data =>
            {
                int rank = (data != null && data.Valid && data.PlayerScore != null) ? data.PlayerScore.rank : 0;
                Debug.Log("[Rank] My rank " + mode + ": " + rank + " / ~" + (data != null ? data.ApproximateCount : 0));
                onRank(rank);
            });
#else
        onRank(0);
#endif
    }

    // 로그인 직후 — 기기에 저장된 최고 기록 중 아직 서버에 없는 것을 올림
    // (1.4.0 이전부터 쌓인 기록, 오프라인으로 세운 기록 포함)
    private static void SyncBestScores()
    {
        foreach (GameMode mode in Enum.GetValues(typeof(GameMode)))
        {
            int best = GameModeManager.GetBestScore(mode);
            if (best > GetSynced(mode)) SubmitScore(mode, best);
        }
    }

    private static int GetSynced(GameMode mode) => PlayerPrefs.GetInt(KeySyncedPrefix + mode, 0);

    private static void SetSynced(GameMode mode, int score)
    {
        PlayerPrefs.SetInt(KeySyncedPrefix + mode, score);
        PlayerPrefs.Save();
    }

    // ===== 랭킹 화면 =====
    // 메인 메뉴: 전체 리더보드 목록
    public static void ShowAll()
    {
        RunSignedIn(() =>
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayGamesPlatform.Instance.ShowLeaderboardUI();
#endif
        });
    }

    // 게임오버: 해당 모드 리더보드
    public static void Show(GameMode mode)
    {
        RunSignedIn(() =>
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayGamesPlatform.Instance.ShowLeaderboardUI(GetLeaderboardId(mode));
#endif
        });
    }

    // 로그인돼 있으면 바로 실행, 아니면 로그인 창을 띄운 뒤 성공하면 실행
    // (자동 로그인은 Play 게임즈를 처음 쓰는 유저에게는 실패하므로 버튼에서 직접 로그인)
    private static void RunSignedIn(Action action)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (IsSignedIn) { action(); return; }
        if (isSigningIn) return;   // 연타 방지

        isSigningIn = true;
        PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
        {
            OnAuthResult(status);
            if (IsSignedIn) action();
        });
#else
        Debug.Log("[Rank] Editor: leaderboard UI is Android only");
#endif
    }
}