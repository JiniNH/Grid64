using UnityEngine;
using System;
using System.Collections;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

// 구글 Play 인앱 업데이트 (유연한 업데이트)
// - 메인 메뉴에서 앱 실행당 한 번만 확인
// - 출시 후 MinStalenessDays일이 지난 업데이트만 안내 (잦은 팝업 방지)
// - 구글 창에서 "업데이트"를 누르면 플레이하는 동안 백그라운드로 다운로드
// - 다운로드가 끝나면 Ready 이벤트 → 메인 메뉴에서 "재시작" 버튼 표시
// - 스토어에서 설치한 빌드에서만 동작 (내부 테스트 트랙으로 테스트)
public class AppUpdater : MonoBehaviour
{
    private const int MinStalenessDays = 3;

    public static bool IsReady { get; private set; }   // 다운로드 완료, 재시작만 남음
    public static event Action Ready;

    private static AppUpdater instance;
    private static bool checkedThisSession;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AppUpdateManager manager;
#endif

    // MainMenuUI.Start()에서 호출
    public static void CheckOnce()
    {
        if (checkedThisSession) return;
        checkedThisSession = true;

        GetInstance().StartCoroutine(instance.CheckRoutine());
    }

    // "재시작" 버튼에서 호출 — 설치 후 앱이 자동으로 다시 켜짐
    public static void CompleteUpdate()
    {
        if (!IsReady || instance == null) return;
        instance.StartCoroutine(instance.CompleteRoutine());
    }

    private static AppUpdater GetInstance()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("AppUpdater");
            DontDestroyOnLoad(go);   // 게임 씬으로 넘어가도 다운로드 감시 유지
            instance = go.AddComponent<AppUpdater>();
        }
        return instance;
    }

    private static void Log(string msg)
    {
        Debug.Log("[Update] " + msg);
    }

    private IEnumerator CheckRoutine()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        manager = new AppUpdateManager();

        var infoOp = manager.GetAppUpdateInfo();
        yield return infoOp;

        if (!infoOp.IsSuccessful)
        {
            Log("info error: " + infoOp.Error);
            yield break;
        }

        AppUpdateInfo info = infoOp.GetResult();
        int staleness = info.ClientVersionStalenessDays ?? 0;
        Log("availability " + info.UpdateAvailability + ", staleness " + staleness + " days, status " + info.AppUpdateStatus);

        // 지난번에 받아 둔 업데이트가 설치 대기 중이면 바로 재시작 안내
        if (info.AppUpdateStatus == AppUpdateStatus.Downloaded)
        {
            SetReady();
            yield break;
        }

        if (info.UpdateAvailability != UpdateAvailability.UpdateAvailable) yield break;
        if (staleness < MinStalenessDays) { Log("skip - too new"); yield break; }

        var options = AppUpdateOptions.FlexibleAppUpdateOptions();
        if (!info.IsUpdateTypeAllowed(options)) { Log("skip - flexible not allowed"); yield break; }

        // 구글의 업데이트 안내 창 → "업데이트"를 누르면 백그라운드 다운로드
        var request = manager.StartUpdate(info, options);
        while (!request.IsDone) yield return null;   // 플레이 중에도 계속 진행

        Log("request done: status " + request.Status + ", error " + request.Error);
        if (request.Status == AppUpdateStatus.Downloaded) SetReady();
#else
        Log("editor - in-app updates only work in Play Store installs");
        yield break;
#endif
    }

    private void SetReady()
    {
        IsReady = true;
        Log("downloaded - waiting for restart");
        Ready?.Invoke();
    }

    private IEnumerator CompleteRoutine()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var op = manager.CompleteUpdate();
        yield return op;
        // 성공하면 앱이 재시작되어 여기까지 오지 않음
        Log("complete error: " + op.Error);
#else
        yield break;
#endif
    }
}