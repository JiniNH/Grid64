using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.SceneManagement;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    // ===== 테스트 광고 ID (개발 중에는 반드시 이걸 사용!) =====
    private const string TEST_BANNER_ID = "ca-app-pub-3940256099942544/6300978111";
    private const string TEST_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";

    [Header("실제 광고 ID (출시 직전에만 사용)")]
    [SerializeField] private string realBannerId = "ca-app-pub-9494767565019927/6689235225";
    [SerializeField] private string realInterstitialId = "ca-app-pub-9494767565019927/2749990215";

    [Header("테스트 모드 (개발 중에는 반드시 true)")]
    [SerializeField] private bool useTestAds = true;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;

    // 전면광고 노출 빈도 조절
    [SerializeField] private int gameOverCountForAd = 3;   // 게임오버 N회마다 전면광고
    private int gameOverCount = 0;

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
        // 씬 전환으로 사라진 배너를 다시 생성
        LoadBanner();
    }

    void Start()
    {
        // SDK 초기화
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob 초기화 완료");
            LoadBanner();
            LoadInterstitial();
        });
    }

    private string BannerId => useTestAds ? TEST_BANNER_ID : realBannerId;
    private string InterstitialId => useTestAds ? TEST_INTERSTITIAL_ID : realInterstitialId;

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
                LoadInterstitial();
            };
        });
    }

    // 게임오버 시 호출 — N회마다 광고 노출
    public void OnGameOver()
    {
        gameOverCount++;

        if (gameOverCount >= gameOverCountForAd)
        {
            gameOverCount = 0;
            ShowInterstitial();
        }
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.Log("전면광고 준비 안 됨");
            LoadInterstitial();
        }
    }
}