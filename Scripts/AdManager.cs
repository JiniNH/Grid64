using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    // ===== 테스트 광고 ID (개발 중에는 반드시 이걸 사용!) =====
    private const string TEST_BANNER_ID       = "ca-app-pub-3940256099942544/6300978111";
    private const string TEST_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
    private const string TEST_REWARDED_ID     = "ca-app-pub-3940256099942544/5224354917";

    [Header("실제 광고 ID (출시 직전에만 사용)")]
    [SerializeField] private string realBannerId = "YOUR_BANNER_AD_UNIT_ID";
    [SerializeField] private string realInterstitialId = "YOUR_INTERSTITIAL_AD_UNIT_ID";
    [SerializeField] private string realRewardedId = "YOUR_REWARDED_AD_UNIT_ID";   // 실제 값은 인스펙터(프리팹)에 입력

    [Header("테스트 모드 (개발 중에는 반드시 true)")]
    [SerializeField] private bool useTestAds = true;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    // 전면광고 노출 빈도 조절
    [SerializeField] private int gameOverCountForAd = 2;         // 게임오버 N회마다 전면광고
    [SerializeField] private float minPlaySecondsForQuit = 30f;  // 이보다 짧게 한 판은 다시하기·나가기를 카운트하지 않음
    [SerializeField] private float minSecondsBetweenAds = 60f;   // 전면 광고 최소 간격
    private int gameOverCount = 0;
    private float lastInterstitialTime = -999f;

    // 부활한 판은 게임오버 때 전면광고 생략 (한 판에 광고 두 번 방지)
    private bool skipNextInterstitial = false;

    // 로드 실패 시 재시도 (5 → 10 → 20 → 40 → 60초, 이후 60초 간격)
    private bool interstitialLoading, rewardedLoading;
    private int interstitialRetry, rewardedRetry;

    // 배너
    private bool adsInitialized;     // SDK 초기화 끝났는지
    private int bannerRetry;
    private int bannerRequestId;     // 예전 요청의 재시도가 새 배너를 지우지 않게

    // 전면·보상형 광고가 화면을 덮고 있는 중인지 (PauseMenu가 참고)
    public bool IsShowingFullScreenAd { get; private set; }

    // 보상형 광고를 바로 보여줄 수 있는지 (오프라인이면 false)
    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();

    // ※ 로그는 영어로 작성 — 폰 로그(Android Logcat)에서 한글이 깨지기 때문

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬이 로드될 때마다 배너를 다시 만들도록 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 로드될 때마다 호출됨
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 판이 시작되므로 부활 기록 초기화
        skipNextInterstitial = false;

        // 초기화 전이면 초기화 콜백에서 로드하므로 생략 (첫 진입 때 두 번 요청 방지)
        if (adsInitialized) LoadBanner();
    }

    void Start()
    {
        // 광고 콜백을 Unity 메인 스레드에서 받음 (UI·보드 조작 시 에러 방지)
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // SDK 초기화
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[Ad] AdMob initialized");
            adsInitialized = true;
            LoadBanner();
            LoadInterstitial();
            LoadRewarded();
        });
    }

    private string BannerId       => useTestAds ? TEST_BANNER_ID       : realBannerId;
    private string InterstitialId => useTestAds ? TEST_INTERSTITIAL_ID : realInterstitialId;
    private string RewardedId     => useTestAds ? TEST_REWARDED_ID     : realRewardedId;

    // ===== 배너 =====
    public void LoadBanner()
    {
        if (bannerView != null) bannerView.Destroy();

        int requestId = ++bannerRequestId;

        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        bannerView = new BannerView(BannerId, adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            bannerRetry = 0;
            Debug.Log("[Ad] Banner loaded");
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            if (requestId != bannerRequestId) return;   // 이미 새 배너로 교체됨

            bannerRetry++;
            float wait = RetryDelay(bannerRetry);
            Debug.LogWarning($"[Ad] Banner load failed #{bannerRetry} (code {error?.GetCode()}) -> retry in {wait:F0}s");

            StartCoroutine(RetryAfter(wait, () =>
            {
                if (requestId == bannerRequestId) LoadBanner();
            }));
        };

        bannerView.LoadAd(new AdRequest());
        Debug.Log("[Ad] Banner requested");
    }

    public void ShowBanner() => bannerView?.Show();
    public void HideBanner() => bannerView?.Hide();

    // ===== 전면 광고 =====
    public void LoadInterstitial()
    {
        // 로드 중이거나 이미 준비된 광고가 있으면 생략 (재시도 예약과 겹침 방지)
        if (interstitialLoading) return;
        if (interstitialAd != null && interstitialAd.CanShowAd()) return;

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        interstitialLoading = true;

        InterstitialAd.Load(InterstitialId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            interstitialLoading = false;

            if (error != null || ad == null)
            {
                interstitialRetry++;
                float wait = RetryDelay(interstitialRetry);
                Debug.LogWarning($"[Ad] Interstitial load failed #{interstitialRetry} (code {error?.GetCode()}: {error?.GetMessage()}) -> retry in {wait:F0}s");
                StartCoroutine(RetryAfter(wait, LoadInterstitial));
                return;
            }

            interstitialRetry = 0;
            interstitialAd = ad;
            Debug.Log("[Ad] Interstitial loaded");

            interstitialAd.OnAdFullScreenContentOpened += () => Debug.Log("[Ad] Interstitial opened");

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[Ad] Interstitial closed");
                IsShowingFullScreenAd = false;
                LoadInterstitial();
            };

            interstitialAd.OnAdFullScreenContentFailed += (AdError e) =>
            {
                Debug.LogWarning("[Ad] Interstitial show failed: " + e?.GetMessage());
                IsShowingFullScreenAd = false;
                LoadInterstitial();
            };
        });
    }

    // 게임오버 시 호출 — N회마다 전면 광고. 이번에 실제로 띄웠으면 true
    public bool OnGameOver()
    {
        // 부활 광고를 본 판이면 이번 게임오버는 건너뜀
        if (skipNextInterstitial)
        {
            skipNextInterstitial = false;
            Debug.Log("[Ad] Interstitial skipped - revived this game");
            return false;
        }

        gameOverCount++;
        Debug.Log($"[Ad] Game over count {gameOverCount}/{gameOverCountForAd}");

        if (gameOverCount >= gameOverCountForAd)
        {
            // 직전 광고와 너무 가까우면 이번은 건너뛰고 다음 게임오버 때 다시 시도
            if (Time.realtimeSinceStartup - lastInterstitialTime < minSecondsBetweenAds)
            {
                Debug.Log("[Ad] Interstitial skipped - too soon after last ad");
                return false;
            }

            bool shown = ShowInterstitial();
            if (shown)
            {
                gameOverCount = 0;   // 실제로 띄웠을 때만 초기화
                lastInterstitialTime = Time.realtimeSinceStartup;
            }
            return shown;
        }
        return false;
    }

    // 일시정지 메뉴에서 다시 시작·메인 메뉴로 나갈 때 — 광고는 띄우지 않고 카운트만 올림
    // 카운트가 기준을 넘으면 다음 게임오버 때 전면 광고가 나옴
    public void OnQuitFromPause(float playedSeconds)
    {
        if (playedSeconds < minPlaySecondsForQuit)
        {
            Debug.Log($"[Ad] Quit from pause after {playedSeconds:F0}s - too short, not counted");
            return;
        }

        gameOverCount++;
        Debug.Log($"[Ad] Quit from pause - count {gameOverCount}/{gameOverCountForAd}");
    }

    public bool ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            IsShowingFullScreenAd = true;
            Debug.Log("[Ad] Showing interstitial (game over)");
            interstitialAd.Show();
            return true;
        }

        Debug.Log("[Ad] Interstitial not ready");
        LoadInterstitial();
        return false;
    }

    // ===== 보상형 광고 (부활) =====
    public void LoadRewarded()
    {
        if (rewardedLoading) return;
        if (rewardedAd != null && rewardedAd.CanShowAd()) return;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        rewardedLoading = true;

        RewardedAd.Load(RewardedId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            rewardedLoading = false;

            if (error != null || ad == null)
            {
                rewardedRetry++;
                float wait = RetryDelay(rewardedRetry);
                Debug.LogWarning($"[Ad] Rewarded load failed #{rewardedRetry} (code {error?.GetCode()}: {error?.GetMessage()}) -> retry in {wait:F0}s");
                StartCoroutine(RetryAfter(wait, LoadRewarded));
                return;
            }

            rewardedRetry = 0;
            rewardedAd = ad;
            Debug.Log("[Ad] Rewarded loaded");
        });
    }

    // 보상형 광고 표시 — 끝까지 보면 onRewarded, 중간에 닫거나 실패하면 onFailed
    // 두 콜백 모두 광고가 닫힌 뒤(게임 화면으로 돌아온 뒤) 호출됨
    public void ShowRewarded(Action onRewarded, Action onFailed)
    {
        if (!IsRewardedReady)
        {
            Debug.Log("[Ad] Rewarded not ready");
            onFailed?.Invoke();
            LoadRewarded();
            return;
        }

        bool earned = false;
        IsShowingFullScreenAd = true;
        Debug.Log("[Ad] Showing rewarded (revive)");

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log($"[Ad] Rewarded closed (earned: {earned})");
            IsShowingFullScreenAd = false;

            if (earned)
            {
                skipNextInterstitial = true;
                onRewarded?.Invoke();
            }
            else
            {
                onFailed?.Invoke();
            }

            LoadRewarded();   // 다음 판을 위해 미리 로드
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError e) =>
        {
            Debug.LogWarning("[Ad] Rewarded show failed: " + e?.GetMessage());
            IsShowingFullScreenAd = false;
            onFailed?.Invoke();
            LoadRewarded();
        };

        // 끝까지 시청하면 보상 표시만 해두고, 실제 부활은 광고가 닫힌 뒤에 처리
        rewardedAd.Show((Reward reward) => { earned = true; });
    }

    // 재시도 간격: 5, 10, 20, 40, 60, 60 … 초
    private static float RetryDelay(int attempt) => Mathf.Min(5f * Mathf.Pow(2, attempt - 1), 60f);

    // 게임오버 중(timeScale 0)에도 흐르도록 실제 시간 기준으로 대기
    private IEnumerator RetryAfter(float seconds, Action load)
    {
        yield return new WaitForSecondsRealtime(seconds);
        load();
    }
}