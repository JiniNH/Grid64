using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.SceneManagement;
using System;

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
    [SerializeField] private string realRewardedId = "YOUR_REWARDED_AD_UNIT_ID";   // AdMob에서 보상형 광고 단위 생성 후 입력

    [Header("테스트 모드 (개발 중에는 반드시 true)")]
    [SerializeField] private bool useTestAds = true;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    // 전면광고 노출 빈도 조절
    [SerializeField] private int gameOverCountForAd = 3;   // 게임오버 N회마다 전면광고
    private int gameOverCount = 0;

    // 부활한 판은 게임오버 때 전면광고 생략 (한 판에 광고 두 번 방지)
    private bool skipNextInterstitial = false;

    // 전면·보상형 광고가 화면을 덮고 있는 중인지 (PauseMenu가 참고)
    public bool IsShowingFullScreenAd { get; private set; }

    // 보상형 광고를 바로 보여줄 수 있는지 (오프라인이면 false)
    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();

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

        // 씬 전환으로 사라진 배너를 다시 생성
        LoadBanner();
    }

    void Start()
    {
        // 광고 콜백을 Unity 메인 스레드에서 받음 (UI·보드 조작 시 에러 방지)
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // SDK 초기화
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob 초기화 완료");
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

        // 화면 하단에 적응형 배너 생성
        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        bannerView = new BannerView(BannerId, adaptiveSize, AdPosition.Bottom);

        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);

        Debug.Log("배너 광고 로드 요청");
    }

    public void ShowBanner() => bannerView?.Show();
    public void HideBanner() => bannerView?.Hide();

    // ===== 전면 광고 =====
    public void LoadInterstitial()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest request = new AdRequest();

        InterstitialAd.Load(InterstitialId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("전면광고 로드 실패: " + error);
                return;
            }

            interstitialAd = ad;
            Debug.Log("전면광고 로드 완료");

            // 광고가 닫히면 다음 광고를 미리 로드
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("전면광고 닫힘");
                IsShowingFullScreenAd = false;
                LoadInterstitial();
            };

            interstitialAd.OnAdFullScreenContentFailed += (AdError e) =>
            {
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
            return false;
        }

        gameOverCount++;

        if (gameOverCount >= gameOverCountForAd)
        {
            gameOverCount = 0;
            return ShowInterstitial();
        }
        return false;
    }

    public bool ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            IsShowingFullScreenAd = true;
            Debug.Log("▶ 전면광고 표시 (게임오버)");
            interstitialAd.Show();
            return true;
        }

        Debug.Log("전면광고 준비 안 됨");
        LoadInterstitial();
        return false;
    }

    // ===== 보상형 광고 (부활) =====
    public void LoadRewarded()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();

        RewardedAd.Load(RewardedId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("보상형 광고 로드 실패: " + error);
                return;
            }

            rewardedAd = ad;
            Debug.Log("보상형 광고 로드 완료");
        });
    }

    // 보상형 광고 표시 — 끝까지 보면 onRewarded, 중간에 닫거나 실패하면 onFailed
    // 두 콜백 모두 광고가 닫힌 뒤(게임 화면으로 돌아온 뒤) 호출됨
    public void ShowRewarded(Action onRewarded, Action onFailed)
    {
        if (!IsRewardedReady)
        {
            onFailed?.Invoke();
            LoadRewarded();
            return;
        }

        bool earned = false;
        IsShowingFullScreenAd = true;
        Debug.Log("▶ 보상형 광고 표시 (부활)");

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
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
            Debug.LogError("보상형 광고 표시 실패: " + e);
            IsShowingFullScreenAd = false;
            onFailed?.Invoke();
            LoadRewarded();
        };

        // 끝까지 시청하면 보상 표시만 해두고, 실제 부활은 광고가 닫힌 뒤에 처리
        rewardedAd.Show((Reward reward) => { earned = true; });
    }
}