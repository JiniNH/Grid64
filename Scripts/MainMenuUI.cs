using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("씬 설정")]
    public string gameSceneName = "MainGame";

    [Header("버튼")]
    public Button classicButton;
    public Button timeAttackButton;

    [Header("최고 기록 표시")]
    public TextMeshProUGUI classicBestText;
    public TextMeshProUGUI timeAttackBestText;

    [Header("타이틀 연출")]
    public Transform titleTransform;

    [Header("버전 표시")]
    public TextMeshProUGUI versionText;
    [Tooltip("폴더블 진단용: 버전 옆에 실제 해상도 표시 (출시 전 끌 것)")]
    public bool showResolution = false;

    [Header("설정 패널")]
    public Button settingsButton;
    public GameObject settingsPanel;
    public Button settingsCloseButton;
    public Button privacyButton;
    public string privacyUrl = "https://jininh.github.io/jiniworks-privacy/grid64.html";
    public Button licenseButton;
    public string licenseUrl = "https://jininh.github.io/jiniworks-privacy/grid64-licenses.html";

    void Start()
    {
        // [언어] 언어가 바뀌면 최고 기록 앞의 글자("최고 기록" ↔ "Best")를 다시 씀
        Localization.Load();
        Localization.Changed += RefreshBestScores;

        SoundSettings.Load();
        RefreshBestScores();

        if (classicButton != null)
            classicButton.onClick.AddListener(() => StartGame(GameMode.Classic));

        if (timeAttackButton != null)
            timeAttackButton.onClick.AddListener(() => StartGame(GameMode.TimeAttack));

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(CloseSettings);
        if (privacyButton != null) privacyButton.onClick.AddListener(() => Application.OpenURL(privacyUrl));
        if (licenseButton != null) licenseButton.onClick.AddListener(() => Application.OpenURL(licenseUrl));

        UpdateVersionText();
    }

    void OnDestroy()
    {
        // [언어] 씬을 떠날 때 구독 해제 (사라진 텍스트에 쓰려다 에러 나는 것 방지)
        Localization.Changed -= RefreshBestScores;
    }

    void Update()
    {
        // 타이틀이 천천히 숨쉬는 연출
        if (titleTransform != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 1.8f) * 0.03f;
            titleTransform.localScale = Vector3.one * pulse;
        }

        // 폴더블 접기·펼치기 시 해상도 변화 확인용
        if (showResolution) UpdateVersionText();

        // 안드로이드 뒤로가기: 설정이 열려 있으면 닫기
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SoundSettings.Flush();
    }

    private void UpdateVersionText()
    {
        if (versionText == null) return;

        versionText.text = showResolution
            ? $"v{Application.version}  {Screen.width}x{Screen.height}"
            : "v" + Application.version;
    }

    private void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        SoundSettings.Flush();
    }

    // 저장된 기록을 읽어서 표시만 함 (왕관 아이콘이 "최고 기록"을 대신함)
    private void RefreshBestScores()
    {
        if (classicBestText != null)
            classicBestText.text = GameModeManager.GetBestScore(GameMode.Classic).ToString("N0");

        if (timeAttackBestText != null)
            timeAttackBestText.text = GameModeManager.GetBestScore(GameMode.TimeAttack).ToString("N0");
    }

    private void StartGame(GameMode mode)
    {
        GameModeManager.Current = mode;

        if (RetroAudio.Instance != null) RetroAudio.Instance.PlayDrop();

        SceneManager.LoadScene(gameSceneName);
    }
}