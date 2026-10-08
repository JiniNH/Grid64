using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
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
    public int comboBonusStart = 3;         // 이 콤보부터 추가 시간 (2콤보는 보너스 없음)
    public float bonusPerCombo = 1f;        // 콤보 단계당 추가 시간 (2콤보부터)
    public float maxComboBonus = 3f;        // 콤보 추가 시간 상한
    public float deadlockPenalty = 10f;     // 데드락 시 차감

    [Header("데드락 구제")]
    public int rescueRows = 3;              // 비워줄 줄 수 (가로·세로 공통)
    public int minRescueCells = 6;          // 이보다 적게 지워지는 후보는 가급적 피함
    public int maxRescueAttempts = 3;       // 구제 후에도 막히면 재시도하는 최대 횟수

    [Header("스피드 업")]
    public float speedUpStartDelay = 60f;     // 이 시간 동안은 원래 속도 (첫 스피드 업 시점)
    public float speedUpInterval = 30f;       // 그 이후 이 간격마다 한 단계씩
    public float speedStep = 0.1f;            // 단계당 시간 흐름 10%
    public int maxSpeedLevel = 5;             // 최대 5단계 (1.5배)
    public float bgmPitchPerLevel = 0.03f;    // 단계당 BGM 빠르기 3%
    public TextMeshProUGUI speedUpText;       // "SPEED UP!" 알림 (평소 꺼둠)
    public TextMeshProUGUI speedLevelText;    // 현재 속도 "×1.2" (선택)
    public float ScoreMultiplier => 1f + speedStep * speedLevel;    // SPEED UP 단계만큼 점수 배수 (×1.0 ~ ×1.5)

    private float elapsedPlayTime = 0f;       // 카운트다운 시작 후 흐른 시간 (일시정지 제외)
    private int speedLevel = 0;

    [Header("경고 연출")]
    public float warningThreshold = 10f;    // 이 시간 이하부터 경고
    public Color normalColor = new Color(0.29f, 0.62f, 0.85f);  // #4A9FD8
    public Color warningColor = new Color(1f, 0.42f, 0.42f);    // #FF6B6B
    public float warningPitchBoost = 1.05f; // 경고 구간에서 BGM에 추가로 곱해지는 값
    private bool wasWarning = false;        // 경고 구간 진입·이탈 감지용

    [Header("UI (선택)")]
    public Image timerBar;                  // 기본 구간 (0 ~ startTime)
    public Image overflowBar;               // 초과 구간 (startTime ~ maxTime)
    public TextMeshProUGUI timerText;       // 남은 초 표시
    public TextMeshProUGUI rescueText;      // "+2초" 같은 시간 보너스 피드백
    public TextMeshProUGUI feverTextRef;    // 타임어택에서 숨길 피버 텍스트

    private float remainingTime;
    private bool isRunning = false;             // 타임어택 판이 진행 중인지 (게임오버 전까지 true)
    private bool waitingForFirstGrab = true;    // 첫 블록을 집기 전이면 시간이 흐르지 않음

    private Vector3 feedbackBasePos;        // 피드백 글자의 원래 위치 (한 번만 기록)
    private bool feedbackEmphasis;          // 이번 피드백이 페널티(강조)인지

    public bool IsRunning => isRunning;
    public bool IsWaitingForStart => waitingForFirstGrab;
    public float RemainingTime => remainingTime;

    void Awake()
    {
        if (GameModeManager.Current != GameMode.TimeAttack)
        {
            // 클래식에서는 타이머 UI만 숨김 (게이지 바는 피버가 사용)
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (rescueText != null) rescueText.gameObject.SetActive(false);
            if (overflowBar != null) overflowBar.gameObject.SetActive(false);
            if (speedUpText != null) speedUpText.gameObject.SetActive(false);
            if (speedLevelText != null) speedLevelText.gameObject.SetActive(false);

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
        waitingForFirstGrab = true;
        UpdateTimerUI();

        // 피드백 글자의 원래 위치를 한 번만 기록 (연속 표시 시 위로 밀리는 것 방지)
        if (rescueText != null) feedbackBasePos = rescueText.transform.localPosition;
        if (speedUpText != null) speedUpText.gameObject.SetActive(false);
        if (speedLevelText != null) speedLevelText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isRunning) return;

        // 첫 블록을 집기 전: 시간은 멈추고 숫자만 천천히 깜빡임
        if (waitingForFirstGrab)
        {
            UpdateWaitingHint();
            return;
        }

        // 일정 시간마다 한 단계씩 빨라짐
        elapsedPlayTime += Time.deltaTime;

        // 첫 구간은 원래 속도, 그 이후 일정 간격마다 한 단계씩
        int targetLevel = 0;
        if (elapsedPlayTime >= speedUpStartDelay)
            targetLevel = Mathf.Min(1 + Mathf.FloorToInt((elapsedPlayTime - speedUpStartDelay) / speedUpInterval), maxSpeedLevel);

        if (targetLevel > speedLevel)
        {
            speedLevel = targetLevel;
            OnSpeedUp();
        }

        float drain = 1f + speedStep * speedLevel;
        remainingTime -= Time.deltaTime * drain;

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

    // 첫 블록을 집는 순간 호출 (BlockDrag에서) — 여러 번 불려도 한 번만 동작
    public void BeginCountdown()
    {
        if (!isRunning || !waitingForFirstGrab) return;

        waitingForFirstGrab = false;
        UpdateTimerUI();   // 깜빡이던 색을 정상으로 되돌림
        Debug.Log("⏱ 타임어택 카운트다운 시작");
    }

    // 대기 중 타이머 숫자가 숨쉬듯 깜빡임 (시작 전이라는 신호)
    private void UpdateWaitingHint()
    {
        if (timerText == null) return;

        float alpha = 0.45f + (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.275f;   // 0.45 ~ 1
        Color c = normalColor;
        c.a = alpha;
        timerText.color = c;
    }

    // 라인 클리어 시 시간 보너스 (BoardManager에서 호출)
    public void AddTimeBonus(int linesCleared, bool isPerfect, int combo)
    {
        if (!isRunning) return;

        float bonus = 0f;
        if (linesCleared == 1) bonus = bonusOneLine;
        else if (linesCleared == 2) bonus = bonusTwoLines;
        else if (linesCleared >= 3) bonus = bonusThreeLines;

        if (isPerfect) bonus += bonusPerfect;

        // 3콤보부터 추가 시간 (자주 나오는 2콤보는 기본 보너스만)
        if (combo >= comboBonusStart)
            bonus += Mathf.Min((combo - comboBonusStart + 1) * bonusPerCombo, maxComboBonus);

        remainingTime = Mathf.Min(remainingTime + bonus, maxTime);
        ShowFeedback(Localization.Format("time_bonus", bonus), normalColor);

        Debug.Log($"⏱ 시간 +{bonus:F0}초 (콤보 {combo}, 남은 시간 {remainingTime:F1}초)");
    }

    // 데드락 발생 시 페널티와 보드 구제 (BoardManager에서 호출)
    // 구제 후에도 막혀 있으면 한 번 더 페널티를 내고 다시 구제 (최대 횟수 제한)
    public void OnDeadlock(List<GameObject> deckBlocks)
    {
        if (!isRunning) return;

        float totalPenalty = 0f;
        bool resolved = false;

        for (int attempt = 0; attempt < maxRescueAttempts; attempt++)
        {
            remainingTime -= deadlockPenalty;
            totalPenalty += deadlockPenalty;

            if (remainingTime <= 0f) break;

            if (BoardManager.Instance.RescueBoard(rescueRows, minRescueCells, deckBlocks))
            {
                resolved = true;
                break;
            }

            Debug.Log($"⚠ 구제 후에도 데드락 — 추가 페널티 ({attempt + 1}/{maxRescueAttempts})");
        }

        // 여러 번 차감돼도 합계로 한 번만 표시
        ShowPenalty(Localization.Format("time_penalty", totalPenalty));

        if (!resolved || remainingTime <= 0f)
        {
            // 시간이 다 됐거나 최대 횟수를 넘겨도 안 풀리면, 멈춘 채 두지 않고 종료
            remainingTime = Mathf.Max(0f, remainingTime);
            isRunning = false;
            UpdateTimerUI();
            BoardManager.Instance.ShowGameOver();
            return;
        }

        UpdateTimerUI();
        Debug.Log($"💥 데드락 구제! -{totalPenalty:F0}초 (남은 시간 {remainingTime:F1}초)");
    }

    // 게임오버 시 타이머 정지 (BoardManager에서 호출)
    public void StopTimer()
    {
        isRunning = false;

        StopCoroutine(nameof(SpeedUpRoutine));
        if (speedUpText != null) speedUpText.gameObject.SetActive(false);
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
        
        // 경고 구간에 들어가거나 벗어날 때 BGM 빠르기 다시 계산
        if (isWarning != wasWarning && isRunning && !waitingForFirstGrab)
        {
            wasWarning = isWarning;
            ApplyBgmPitch(0.5f);
        }
    }

    // "+2초" / "-10초" — 타이머 아래 같은 자리에 표시
    private void ShowFeedback(string message, Color color, bool emphasis = false)
    {
        if (rescueText == null) return;

        StopCoroutine(nameof(FeedbackRoutine));

        // 진행 중이던 연출을 끊었으면 위치·크기를 원래대로 되돌린 뒤 시작
        rescueText.transform.localPosition = feedbackBasePos;
        rescueText.transform.localScale = Vector3.one;

        rescueText.text = message;
        rescueText.color = color;
        rescueText.gameObject.SetActive(true);

        feedbackEmphasis = emphasis;
        StartCoroutine(nameof(FeedbackRoutine));
    }

    // 데드락 페널티 — 같은 자리에서 더 크고 오래
    private void ShowPenalty(string message)
    {
        ShowFeedback(message, warningColor, true);
    }

    private IEnumerator FeedbackRoutine()
    {
        Transform t = rescueText.transform;
        Color original = rescueText.color;

        // 페널티는 크게 튀어오르고 더 오래 머묾
        float duration  = feedbackEmphasis ? 1.3f : 0.8f;
        float popScale  = feedbackEmphasis ? 1.6f : 1.15f;
        float popTime   = 0.15f;
        float riseDist  = feedbackEmphasis ? 30f : 40f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = elapsed / duration;

            // 처음 잠깐 커졌다가 제 크기로
            float scale = elapsed < popTime
                ? Mathf.Lerp(popScale, 1f, elapsed / popTime)
                : 1f;
            t.localScale = Vector3.one * scale;

            // 위로 떠오르며, 뒤쪽 절반 동안 사라짐
            t.localPosition = feedbackBasePos + Vector3.up * (p * riseDist);
            float alpha = p < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.5f) / 0.5f);
            rescueText.color = new Color(original.r, original.g, original.b, alpha);

            yield return null;
        }

        t.localPosition = feedbackBasePos;
        t.localScale = Vector3.one;
        rescueText.gameObject.SetActive(false);
    }

    // ===== 스피드 업 =====

    private void OnSpeedUp()
    {
        float multiplier = 1f + speedStep * speedLevel;

        if (speedLevelText != null)
        {
            speedLevelText.gameObject.SetActive(true);
            speedLevelText.text = "×" + multiplier.ToString("0.0");
        }

        // 단계가 오를수록 높은 음
        if (RetroAudio.Instance != null) RetroAudio.Instance.PlayClear(6 + speedLevel);

        if (speedUpText != null)
        {
            StopCoroutine(nameof(SpeedUpRoutine));
            StartCoroutine(nameof(SpeedUpRoutine));
        }

        // 단계가 오를 때마다 BGM도 빨라짐
        ApplyBgmPitch(0.8f);

        Debug.Log($"⚡ SPEED UP! {speedLevel}단계 — 시간 흐름 ×{multiplier:0.0}");
    }

    // BGM 빠르기 = 스피드 단계 기본값 × (경고 구간이면 추가 상승)
    private void ApplyBgmPitch(float duration)
    {
        if (BGMPlayer.Instance == null) return;

        float pitch = 1f + bgmPitchPerLevel * speedLevel;
        if (wasWarning) pitch *= warningPitchBoost;

        BGMPlayer.Instance.SetPitch(pitch, duration);
    }

    // "SPEED UP!"이 튀어올랐다가 사라지는 연출
    private IEnumerator SpeedUpRoutine()
    {
        speedUpText.text = Localization.Get("speed_up");
        speedUpText.gameObject.SetActive(true);

        Transform t = speedUpText.transform;
        Color baseColor = speedUpText.color;
        baseColor.a = 1f;
        speedUpText.color = baseColor;

        // 작게 시작 → 크게 튀어오름 → 제 크기
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.3f, elapsed / 0.2f);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, elapsed / 0.15f);
            yield return null;
        }

        yield return new WaitForSeconds(0.9f);

        // 서서히 사라짐
        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            Color c = baseColor;
            c.a = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
            speedUpText.color = c;
            yield return null;
        }

        speedUpText.gameObject.SetActive(false);
        t.localScale = Vector3.one;
        speedUpText.color = baseColor;
    }
        
}