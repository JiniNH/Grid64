using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TimeAttackManager : MonoBehaviour
{
    public static TimeAttackManager Instance;

    [Header("시간 설정")]
    public float startTime = 60f;           // 초기 제한 시간
    public float maxTime = 90f;             // 시간 상한 (무한 연장 방지)
    public float bonusOneLine = 2f;         // 1줄 클리어
    public float bonusTwoLines = 5f;        // 2줄 동시
    public float bonusThreeLines = 8f;      // 3줄 이상
    public float bonusPerfect = 3f;         // 교차 클리어 추가
    public float deadlockPenalty = 10f;     // 데드락 시 차감

    [Header("데드락 구제")]
    public int rescueRows = 3;              // 비워줄 줄 수
    public int minRescueCells = 6;          // 이보다 적게 지워지면 하단으로 대체

    [Header("경고 연출")]
    public float warningThreshold = 10f;    // 이 시간 이하부터 경고
    public Color normalColor = new Color(0.29f, 0.62f, 0.85f);  // #4A9FD8
    public Color warningColor = new Color(1f, 0.42f, 0.42f);    // #FF6B6B

    [Header("UI (선택)")]
    public Image timerBar;                  // 기본 구간 (0 ~ startTime)
    public Image overflowBar;               // 초과 구간 (startTime ~ maxTime)
    public TextMeshProUGUI timerText;       // 남은 초 표시
    public TextMeshProUGUI rescueText;      // "-10s" 등 피드백
    public TextMeshProUGUI penaltyText;     // 보드 중앙의 큰 페널티 표시
    public TextMeshProUGUI feverTextRef;    // 타임어택에서 숨길 피버 텍스트

    private float remainingTime;
    private bool isRunning = false;

    public bool IsRunning => isRunning;
    public float RemainingTime => remainingTime;

    void Awake()
    {
        if (GameModeManager.Current != GameMode.TimeAttack)
        {
            // 클래식에서는 타이머 UI만 숨김 (게이지 바는 피버가 사용)
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (rescueText != null) rescueText.gameObject.SetActive(false);
            if (penaltyText != null) penaltyText.gameObject.SetActive(false);
            if (overflowBar != null) overflowBar.gameObject.SetActive(false);

            gameObject.SetActive(false);
            return;
        }

        Instance = this;

        // 타임어택에서는 피버를 사용하지 않으므로 관련 UI를 숨김
        if (feverTextRef != null) feverTextRef.gameObject.SetActive(false);
    }

    void Start()
    {
        remainingTime = startTime;
        isRunning = true;
        UpdateTimerUI();
    }

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            UpdateTimerUI();

            BoardManager.Instance.ShowGameOver();
            return;
        }

        UpdateTimerUI();
    }

    // 라인 클리어 시 시간 보너스 (BoardManager에서 호출)
    public void AddTimeBonus(int linesCleared, bool isPerfect)
    {
        if (!isRunning) return;

        float bonus = 0f;
        if (linesCleared == 1) bonus = bonusOneLine;
        else if (linesCleared == 2) bonus = bonusTwoLines;
        else if (linesCleared >= 3) bonus = bonusThreeLines;

        if (isPerfect) bonus += bonusPerfect;

        remainingTime = Mathf.Min(remainingTime + bonus, maxTime);
        ShowFeedback($"+{bonus:F0}s", normalColor);

        Debug.Log($"⏱ 시간 +{bonus:F0}초 (남은 시간 {remainingTime:F1}초)");
    }

    // 데드락 발생 시 페널티와 보드 구제 (BoardManager에서 호출)
    public void OnDeadlock()
    {
        if (!isRunning) return;

        remainingTime -= deadlockPenalty;
        ShowPenalty($"-{deadlockPenalty:F0}s");

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            UpdateTimerUI();

            BoardManager.Instance.ShowGameOver();
            return;
        }

        BoardManager.Instance.RescueBoard(rescueRows, minRescueCells);
        UpdateTimerUI();

        Debug.Log($"💥 데드락 구제! -{deadlockPenalty:F0}초 (남은 시간 {remainingTime:F1}초)");
    }

    // 게임오버 시 타이머 정지 (BoardManager에서 호출)
    public void StopTimer()
    {
        isRunning = false;
    }

    private void UpdateTimerUI()
    {
        bool isWarning = remainingTime <= warningThreshold;

        if (timerBar != null)
        {
            // 기본 구간: startTime까지 채움 (60초에 가득)
            timerBar.fillAmount = Mathf.Clamp01(remainingTime / startTime);
            timerBar.color = isWarning ? warningColor : normalColor;
        }

        if (overflowBar != null)
        {
            // 초과 구간: startTime을 넘은 만큼만 표시
            float overflow = Mathf.Max(0f, remainingTime - startTime);
            float ratio = overflow / Mathf.Max(0.01f, maxTime - startTime);
            overflowBar.fillAmount = Mathf.Clamp01(ratio);

            // 초과분이 없으면 숨김
            overflowBar.gameObject.SetActive(overflow > 0.01f);
        }

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(remainingTime).ToString();
            timerText.color = isWarning ? warningColor : normalColor;

            // 경고 구간에서 두근거리는 연출
            if (isWarning)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.07f;
                timerText.transform.localScale = Vector3.one * pulse;
            }
            else
            {
                timerText.transform.localScale = Vector3.one;
            }
        }
    }

    // "+2s" / "-10s" 같은 짧은 피드백 표시
    private void ShowFeedback(string message, Color color)
    {
        if (rescueText == null) return;

        rescueText.text = message;
        rescueText.color = color;
        rescueText.gameObject.SetActive(true);

        StopCoroutine(nameof(FeedbackRoutine));
        StartCoroutine(nameof(FeedbackRoutine));
    }

    private IEnumerator FeedbackRoutine()
    {
        Vector3 basePos = rescueText.transform.localPosition;
        float duration = 0.8f;
        float elapsed = 0f;
        Color original = rescueText.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // 위로 떠오르며 사라짐
            rescueText.transform.localPosition = basePos + Vector3.up * (t * 40f);
            rescueText.color = new Color(original.r, original.g, original.b, 1f - t);

            yield return null;
        }

        rescueText.transform.localPosition = basePos;
        rescueText.gameObject.SetActive(false);
    }

        // 데드락 페널티를 보드 중앙에 크게 표시
    private void ShowPenalty(string message)
    {
        if (penaltyText == null)
        {
            ShowFeedback(message, warningColor);   // 폴백
            return;
        }

        penaltyText.text = message;
        penaltyText.color = warningColor;
        penaltyText.gameObject.SetActive(true);

        StopCoroutine(nameof(PenaltyRoutine));
        StartCoroutine(nameof(PenaltyRoutine));
    }

    private IEnumerator PenaltyRoutine()
    {
        Transform t = penaltyText.transform;
        Color original = penaltyText.color;

        // 작게 시작 → 크게 튀어오름
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(0.4f, 1.5f, elapsed / 0.2f);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        // 원래 크기로 착지
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.5f, 1f, elapsed / 0.15f);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.6f);

        // 페이드 아웃
        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            penaltyText.color = new Color(original.r, original.g, original.b, 1f - elapsed / 0.4f);
            yield return null;
        }

        penaltyText.gameObject.SetActive(false);
        penaltyText.color = original;
        t.localScale = Vector3.one;
    }
}