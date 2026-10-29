using UnityEngine;
using System;
using System.Collections;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

// 구글 Play 인앱 리뷰 — 게임 안에서 공식 별점 창을 띄움
// 로그는 Logcat 한글 깨짐 때문에 영어로 남김 (필터: "Review]")
public class InAppReview : MonoBehaviour
{
    // ===== 요청 조건 =====
    private const int MinGameOvers = 10;      // 이 횟수 이상 게임오버를 겪은 유저에게만
    private const int CooldownStepDays = 30; // 요청할 때마다 쿨다운이 30일씩 늘어남 (30 → 60 → 90일)
    private const int MaxCooldownDays = 90;  // 최대 90일. 횟수 상한은 없음 (구글도 자체 제한이 있음)
    private const float Delay = 1.0f;        // 게임오버 팝업이 먼저 보인 뒤 표시

    public const string KeyGameOvers = "Review_GameOverCount";
    public const string KeyLastAsked = "Review_LastAskedTicks";
    public const string KeyRequestCount = "Review_RequestCount";

    private static InAppReview instance;
    private bool isRequesting;

    // BoardManager.ShowGameOver()에서 호출
    public static void OnGameOver(bool isNewRecord, bool adSeenThisRun)
    {
        // 게임오버 횟수는 매번 누적
        int count = PlayerPrefs.GetInt(KeyGameOvers, 0) + 1;
        PlayerPrefs.SetInt(KeyGameOvers, count);
        PlayerPrefs.Save();

        int requests = PlayerPrefs.GetInt(KeyRequestCount, 0);

        // 조건을 순서대로 확인하고, 걸린 이유를 로그로 남김
        if (!isNewRecord)               { Log("skip - not a record (game overs " + count + ")"); return; }
        if (adSeenThisRun)              { Log("skip - ad seen this run"); return; }
        if (count < MinGameOvers)       { Log("skip - game overs " + count + "/" + MinGameOvers); return; }

        double daysLeft = CooldownDaysLeft();
        if (daysLeft > 0)               { Log("skip - cooldown " + daysLeft.ToString("0.0") + " days left"); return; }

        Log("conditions met - requesting (#" + (requests + 1) + ")");
        InAppReview runner = GetInstance();
        runner.StartCoroutine(runner.RequestRoutine());
    }

    // 남은 쿨다운 일수 (0 이하면 요청 가능). 요청 횟수가 늘수록 쿨다운도 길어짐 (30 → 60 → 90일)
    private static double CooldownDaysLeft()
    {
        string saved = PlayerPrefs.GetString(KeyLastAsked, "");
        if (!long.TryParse(saved, out long ticks)) return 0;   // 한 번도 안 물어봄

        int requests = PlayerPrefs.GetInt(KeyRequestCount, 1);
        int cooldown = Mathf.Min(CooldownStepDays * Mathf.Max(requests, 1), MaxCooldownDays);

        DateTime last = new DateTime(ticks, DateTimeKind.Utc);
        return cooldown - (DateTime.UtcNow - last).TotalDays;
    }

    // 씬에 따로 배치하지 않아도 처음 필요할 때 스스로 만들어짐
    private static InAppReview GetInstance()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("InAppReview");
            DontDestroyOnLoad(go);   // 다시 시작을 눌러도 요청이 끊기지 않게
            instance = go.AddComponent<InAppReview>();
        }
        return instance;
    }

    private static void Log(string msg)
    {
        Debug.Log("[Review] " + msg);
    }

    private IEnumerator RequestRoutine()
    {
        if (isRequesting) { Log("skip - already requesting"); yield break; }
        isRequesting = true;

        // 창이 실제로 떴는지는 구글이 알려주지 않으므로, 요청한 시점을 기준으로 기록
        PlayerPrefs.SetString(KeyLastAsked, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.SetInt(KeyRequestCount, PlayerPrefs.GetInt(KeyRequestCount, 0) + 1);
        PlayerPrefs.Save();

        // 게임오버 화면은 게임 시간이 멈춰 있으므로 실제 시간으로 대기
        yield return new WaitForSecondsRealtime(Delay);

#if UNITY_ANDROID && !UNITY_EDITOR
        ReviewManager manager = new ReviewManager();

        var request = manager.RequestReviewFlow();
        yield return request;
        if (request.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning("[Review] request flow error: " + request.Error);
            isRequesting = false;
            yield break;
        }

        var launch = manager.LaunchReviewFlow(request.GetResult());
        yield return launch;
        if (launch.Error != ReviewErrorCode.NoError)
            Debug.LogWarning("[Review] launch flow error: " + launch.Error);
        else
            Log("flow finished (Google decides whether the dialog was actually shown)");
#else
        Log("editor - review dialog only appears in Play Store installs");
#endif

        isRequesting = false;
    }
}