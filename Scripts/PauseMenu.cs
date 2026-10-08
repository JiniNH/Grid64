using UnityEngine;
using UnityEngine.UI;

// 인게임 ☰ 일시정지 메뉴
public class PauseMenu : MonoBehaviour
{
    [Header("패널")]
    public GameObject pausePanel;          // 어두운 배경 포함한 일시정지 전체

    [Header("버튼")]
    public Button menuButton;              // 화면 우상단 ☰
    public Button resumeButton;            // 계속하기
    public Button restartButton;           // 다시 시작
    public Button mainMenuButton;          // 메인 메뉴

    public bool IsOpen => pausePanel != null && pausePanel.activeSelf;

    void Awake()
    {
        SoundSettings.Load();

        if (pausePanel != null) pausePanel.SetActive(false);

        if (menuButton != null)     menuButton.onClick.AddListener(Open);
        if (resumeButton != null)   resumeButton.onClick.AddListener(Close);
        if (restartButton != null)  restartButton.onClick.AddListener(OnRestart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    void Update()
    {
        // 안드로이드 뒤로가기 버튼
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    // 앱이 백그라운드로 가면 자동 일시정지 (전화, 다른 앱 전환 등)
    void OnApplicationPause(bool paused)
    {
        if (!paused) return;

        SoundSettings.Flush();
        Open();
    }

    public void Open()
    {
        if (IsOpen || pausePanel == null) return;
        if (IsGameOver()) return;          // 게임오버 팝업 위에는 띄우지 않음

        Time.timeScale = 0f;               // 블록 조작과 타임어택 타이머 정지
        pausePanel.SetActive(true);
    }

    public void Close()
    {
        if (!IsOpen) return;

        pausePanel.SetActive(false);
        SoundSettings.Flush();
        Time.timeScale = 1f;
    }

    private void OnRestart()
    {
        SoundSettings.Flush();
        if (BoardManager.Instance != null) BoardManager.Instance.RestartGame();
    }

    private void OnMainMenu()
    {
        SoundSettings.Flush();
        if (BoardManager.Instance != null) BoardManager.Instance.GoToMenu();
    }

    // 일시정지를 띄우면 안 되는 상황인지
    private bool IsGameOver()
    {
        BoardManager bm = BoardManager.Instance;
        if (bm == null) return false;

        if (bm.gameOverPopup != null && bm.gameOverPopup.activeSelf) return true;   // 게임오버
        if (bm.revivePanel != null && bm.revivePanel.activeSelf) return true;       // 부활 제안 중
        if (AdManager.Instance != null && AdManager.Instance.IsShowingFullScreenAd) return true;   // 광고 시청 중

        return false;
    }
}