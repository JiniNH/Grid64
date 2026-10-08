using UnityEngine;
using System;
using System.Collections;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

// 구글 Play 인앱 리뷰 — 게임 안에서 공식 별점 창을 띄움
public class InAppReview : MonoBehaviour
{
    // ===== 요청 조건 =====
    private const int MinGameOvers = 10;      // 이 횟수 이상 게임오버를 겪은 유저에게만
    private const int CooldownDays = 30;     // 한 번 요청하면 이 기간 동안 다시 묻지 않음
    private const float Delay = 1.0f;        // 게임오버 팝업이 먼저 보인 뒤 표시

    public const string KeyGameOvers = "Review_GameOverCount";
    public const string KeyLastAsked = "Review_LastAskedTicks";

    private static InAppReview instance;
    private bool isRequesting;
    public const string KeyRequestCount = "Review_RequestCount";

    // BoardManager.ShowGameOver()에서 호출
    public static void OnGameOver(bool isNewRecord, bool adSeenThisRun)
    {
        // 게임오버 횟수는 매번 누적
        int count = PlayerPrefs.GetInt(KeyGameOvers, 0) + 1;
        PlayerPrefs.SetInt(KeyGameOvers, count);
        PlayerPrefs.Save();

        if (!isNewRecord) return;            // 기분 좋은 순간에만
        if (adSeenThisRun) return;           // 광고를 본 판은 피함
        if (count < MinGameOvers) return;    // 충분히 해본 유저만
        if (!CooldownPassed()) return;       // 최근에 물어봤으면 쉬기

        InAppReview runner = GetInstance();
        runner.StartCoroutine(runner.RequestRoutine());
    }

    private static bool CooldownPassed()
    {
        string saved = PlayerPrefs.GetString(KeyLastAsked, "");
        if (!long.TryParse(saved, out long ticks)) return true;   // 한 번도 안 물어봄

        DateTime last = new DateTime(ticks, DateTimeKind.Utc);
        return (DateTime.UtcNow - last).TotalDays >= CooldownDays;
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

    private IEnumerator RequestRoutine()
    {
        if (isRequesting) yield break;
        isRequesting = true;

        // 창이 실제로 떴는지는 구글이 알려주지 않으므로, 요청한 시점을 기준으로 기록
        PlayerPrefs.SetString(KeyLastAsked, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();

        // 게임오버 화면은 게임 시간이 멈춰 있으므로 실제 시간으로 대기
        yield return new WaitForSecondsRealtime(Delay);

#if UNITY_ANDROID && !UNITY_EDITOR
        ReviewManager manager = new ReviewManager();

        var request = manager.RequestReviewFlow();
        yield return request;
        if (request.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning("[Review] 요청 실패: " + request.Error);
            isRequesting = false;
            yield break;
        }

        var launch = manager.LaunchReviewFlow(request.GetResult());
        yield return launch;
        if (launch.Error != ReviewErrorCode.NoError)
            Debug.LogWarning("[Review] 표시 실패: " + launch.Error);
#else
        Debug.Log("[Review] 리뷰 요청 — 에디터에서는 창이 뜨지 않음 (Play 스토어 설치본에서만 표시)");
#endif

        isRequesting = false;
    }
}