using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class BoardManager : MonoBehaviour
{
    // 싱글톤: 외부 스크립트에서 쉽게 접근하도록 자기 자신을 저장
    public static BoardManager Instance;

    // =====================================================================
    //  인스펙터 설정
    // =====================================================================

    [Header("보드")]
    public int width = 8;
    public int height = 8;
    public GameObject tilePrefab;
    [Tooltip("보드를 위아래로 미세 조정 (양수 = 위로)")]
    public float boardYOffset = 0f;

    [Header("UI 설정")]
    public TextMeshProUGUI scoreText;       // 인게임 현재 점수
    public GameObject gameOverPopup;        // 게임오버 팝업 패널
    public TextMeshProUGUI finalScoreText;  // 게임오버 팝업의 최종 점수

    [Header("이펙트 설정")]
    public GameObject explosionParticlePrefab;  // 라인 클리어 파편

    [Header("콤보 설정")]
    public int currentCombo = 0;            // 현재 콤보 횟수
    public TextMeshProUGUI comboText;       // 콤보 글자

    [Header("콤보 배수")]
    public float comboStepClassic = 0.1f;      // 클래식: 콤보 1단계당 +10% (기존 값)
    public float comboStepTimeAttack = 0.25f;  // 타임어택: 콤보 1단계당 +25%
    public float maxComboMultiplierTA = 3f;    // 타임어택 콤보 배수 상한

    [Header("피버타임 설정")]
    public float feverGauge = 0f;           // 현재 게이지 (0~100)
    public float feverMax = 100f;           // 최대치
    public float feverChargeCombo = 15f;    // 2콤보 이상 유지 시
    public float feverChargeMulti = 20f;    // 다중 클리어(2줄 이상) 시
    public float feverChargePerfect = 25f;  // Perfect(교차) 시
    public float feverDuration = 10f;       // 피버 지속 시간(초)
    public float feverMultiplier = 2f;      // 피버 중 점수 배수

    [Header("피버 UI (선택)")]
    public Image feverGaugeBar;             // 게이지 바 (Fill 방식)
    public TextMeshProUGUI feverText;       // "피버 타임!" 표시용

    [Header("피버 연출")]
    public SpriteRenderer boardTintTarget;  // 피버 중 물들일 보드 배경 (선택)
    public Color feverTintColor = new Color(1f, 0.92f, 0.85f);  // 보드에 덧입힐 색
    public float feverPitch = 1.08f;        // 피버 중 BGM 재생 속도

    [Header("올클리어 보너스")]
    public int allClearBonus = 3000;        // 올클리어 보너스 점수
    public TextMeshProUGUI allClearText;    // "올 클리어!" 표시용

    [Header("카메라 흔들림")]
    public float shakeMagnitudeClear = 0.08f;     // 일반 라인 클리어 시
    public float shakeMagnitudeCombo = 0.15f;     // 다중/콤보 클리어 시
    public float shakeMagnitudeAllClear = 0.25f;  // 올클리어 시

    [Header("라인 섬광 효과")]
    public float flashDuration = 0.1f;               // 섬광 지속 시간
    [Range(0f, 1f)] public float flashAlpha = 0.22f; // 섬광 최대 밝기

    [Header("게임오버 UI")]
    public TextMeshProUGUI bestScoreText;   // 최고 기록
    public GameObject newRecordBadge;       // 신기록 뱃지
    public TextMeshProUGUI modeLabelText;   // 현재 모드 표시 (선택)
    public string mainMenuSceneName = "MainMenu";

    [Header("기록 공유")]
    public Button shareButton;              // 공유 버튼 (매 게임오버마다 표시)
    public Image shareIcon;                 // 버튼 안 아이콘 (신기록이면 호박색)
    public TextMeshProUGUI shareLabel;      // 버튼 안 글자 (신기록이면 호박색)

    [Header("인게임 최고 기록")]
    public GameObject bestHud;                 // 왕관 + 숫자 묶음 (SafeAreaPanel 안)
    public TextMeshProUGUI bestHudText;        // 최고 기록 숫자
    public TextMeshProUGUI recordBreakText;    // 경신 순간 "신기록!" (평소 꺼둠)
    public float recordHoldTime = 1.2f;        // "신기록!"이 다 보인 채 머무는 시간
    public float recordFadeTime = 0.5f;        // 사라지는 데 걸리는 시간

    [Header("부활 (클래식)")]
    public GameObject revivePanel;          // 부활 제안 패널 (어두운 배경 포함)
    public Button reviveAdButton;           // 광고 보고 이어하기
    public Button reviveDeclineButton;      // 괜찮아요
    public int reviveRescueSize = 3;        // 비워줄 줄 수
    public int reviveMinCells = 6;          // 이보다 적게 지워지는 후보는 가급적 피함

    [Header("부활 카운트다운")]
    public float reviveCountdown = 5f;             // 제안 유지 시간 (초)
    public Image reviveCountdownFill;              // 줄어드는 원형 게이지 (Filled / Radial 360)
    public TextMeshProUGUI reviveCountdownText;    // 남은 초
    public TextMeshProUGUI reviveScoreText;        // 현재 점수

    // =====================================================================
    //  내부 상태
    // =====================================================================

    // 보드 데이터 (외부에서 읽고 씀)
    public int[,] gridData;
    public GameObject[,] boardObjects;
    private float offsetX, offsetY;

    // 점수
    private int currentScore = 0;
    private int bestScore = 0;
    private bool lastGameWasRecord;

    // 연출 코루틴 추적
    private Coroutine comboRoutine;
    private Coroutine feverRoutine;
    private Coroutine shakeRoutine;
    private Coroutine reviveTimerRoutine;

    // 피버
    private bool isFeverActive = false;
    private Color boardOriginalColor;
    private Color feverTextBaseColor;

    // 카메라 흔들림 기준 위치
    private Vector3 camRestPos;

    // 부활
    private bool hasRevived = false;        // 한 판에 한 번만
    private List<GameObject> reviveDeck;    // 데드락 당시 덱 블록

    // 인게임 최고 기록
    private bool recordBroken = false;      // 이번 판에 이미 경신했는지 (연출은 한 번만)
    private int lastHudScore = -1;          // 같은 숫자를 매 프레임 다시 쓰지 않게

    // 파편 색 캐시 (매번 텍스처를 읽지 않도록)
    private Dictionary<Sprite, Color> fragmentColorCache = new Dictionary<Sprite, Color>();

    // =====================================================================
    //  생명주기
    // =====================================================================

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeBoard();
        UpdateFeverUI();

        // 현재 모드의 최고 기록
        bestScore = GameModeManager.GetBestScore(GameModeManager.Current);

        if (modeLabelText != null)
            modeLabelText.text = GameModeManager.GetDisplayName(GameModeManager.Current);
        if (boardTintTarget != null) boardOriginalColor = boardTintTarget.color;
        if (feverText != null) feverTextBaseColor = feverText.color;

        // 부활 패널
        if (revivePanel != null) revivePanel.SetActive(false);
        if (reviveAdButton != null) reviveAdButton.onClick.AddListener(OnReviveAccepted);
        if (reviveDeclineButton != null) reviveDeclineButton.onClick.AddListener(OnReviveDeclined);
        
        // 기록 공유 버튼
        if (shareButton != null)
        {
            shareButton.gameObject.SetActive(false);
            shareButton.onClick.AddListener(ShareRecord);
        }

        // 인게임 최고 기록
        RefreshBestHud();
        if (recordBreakText != null) recordBreakText.gameObject.SetActive(false);
    }

    void Update()
    {
        CheckRecordBreak();
    }

    void InitializeBoard()
    {
        gridData = new int[width, height];
        boardObjects = new GameObject[width, height];

        offsetX = (width - 1) / 2f;
        offsetY = (height - 1) / 2f - boardYOffset;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridData[x, y] = 0;   // 0 = 빈칸
                Vector2 position = new Vector2(x - offsetX, y - offsetY);
                Instantiate(tilePrefab, position, Quaternion.identity);
            }
        }
    }

    // =====================================================================
    //  좌표 변환
    // =====================================================================

    // 월드 좌표 → 배열 인덱스
    public Vector2Int GetGridIndex(Vector2 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + offsetX);
        int y = Mathf.RoundToInt(worldPos.y + offsetY);
        return new Vector2Int(x, y);
    }

    // 배열 인덱스 → 월드 좌표 (스냅용)
    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(x - offsetX, y - offsetY);
    }

    // 보드 안쪽이면서 빈칸인지
    public bool IsValidAndEmpty(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            return gridData[x, y] == 0;
        return false;
    }

    private bool IsBoardEmpty()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (gridData[x, y] != 0) return false;
        return true;
    }

    // =====================================================================
    //  라인 클리어 / 점수
    // =====================================================================

    public void CheckAndClearLines()
    {
        List<Vector2Int> linesToClear = new List<Vector2Int>();

        int rowCleared = 0;
        int colCleared = 0;

        // 가로줄
        for (int y = 0; y < height; y++)
        {
            bool isRowFull = true;
            for (int x = 0; x < width; x++)
            {
                if (gridData[x, y] == 0) { isRowFull = false; break; }
            }
            if (isRowFull)
            {
                rowCleared++;
                for (int x = 0; x < width; x++) linesToClear.Add(new Vector2Int(x, y));
                StartCoroutine(LineFlash(true, y));
            }
        }

        // 세로줄
        for (int x = 0; x < width; x++)
        {
            bool isColFull = true;
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] == 0) { isColFull = false; break; }
            }
            if (isColFull)
            {
                colCleared++;
                for (int y = 0; y < height; y++) linesToClear.Add(new Vector2Int(x, y));
                StartCoroutine(LineFlash(false, x));
            }
        }

        int linesClearedCount = rowCleared + colCleared;

        // 꽉 찬 줄의 블록 파괴 및 배열 초기화
        foreach (Vector2Int pos in linesToClear)
        {
            if (boardObjects[pos.x, pos.y] != null)
            {
                if (explosionParticlePrefab != null)
                {
                    Vector2 effectPos = GetWorldPosition(pos.x, pos.y);
                    GameObject fx = Instantiate(explosionParticlePrefab, effectPos, Quaternion.identity);

                    // 지워지는 블록의 색을 파편에 적용
                    ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
                    if (ps != null)
                    {
                        Color c = GetFragmentColor(boardObjects[pos.x, pos.y]);
                        Color dim = new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, 1f);

                        var main = ps.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(dim, c);
                    }

                    Destroy(fx, 2f);
                }

                Destroy(boardObjects[pos.x, pos.y]);
                boardObjects[pos.x, pos.y] = null;
            }
            gridData[pos.x, pos.y] = 0;
        }

        if (linesClearedCount > 0)
        {
            currentCombo++;
            if (RetroAudio.Instance != null) RetroAudio.Instance.PlayClear(currentCombo);

            // 동시에 많이 지울수록 칸당 가치 상승
            int perCell;
            if (linesClearedCount == 1) perCell = 100;
            else if (linesClearedCount == 2) perCell = 150;
            else perCell = 200;

            int totalCells = linesClearedCount * width;
            bool isTimeAttack = GameModeManager.Current == GameMode.TimeAttack;
            float comboStep = isTimeAttack ? comboStepTimeAttack : comboStepClassic;
            float comboMultiplier = 1f + comboStep * (currentCombo - 1);
            if (isTimeAttack) comboMultiplier = Mathf.Min(comboMultiplier, maxComboMultiplierTA);
            int earnedScore = Mathf.RoundToInt(totalCells * perCell * comboMultiplier);

            // [Perfect] 가로+세로 교차 클리어
            bool isPerfect = (rowCleared > 0 && colCleared > 0);
            if (isPerfect)
            {
                earnedScore = Mathf.RoundToInt(earnedScore * 1.5f);
                Debug.Log($"✨ PERFECT! 교차 클리어 (가로 {rowCleared} + 세로 {colCleared}) → 보너스 1.5배");
            }

            // [피버] 피버 중이면 점수 2배
            if (isFeverActive)
            {
                earnedScore = Mathf.RoundToInt(earnedScore * feverMultiplier);
            }

            // [타임어택] SPEED UP 단계만큼 점수 배수  ← 추가
            if (TimeAttackManager.Instance != null)
            {
                earnedScore = Mathf.RoundToInt(earnedScore * TimeAttackManager.Instance.ScoreMultiplier);
            }

            // [연출] 흔들림 강도 차등
            bool isBigClear = (linesClearedCount >= 2 || currentCombo > 1 || isPerfect);
            StartShake(0.25f, isBigClear ? shakeMagnitudeCombo : shakeMagnitudeClear);

            if (currentCombo > 1)
            {
                ShowComboText(currentCombo);
            }

            AddScore(earnedScore);

            // [타임어택] 시간 보너스
            if (TimeAttackManager.Instance != null)
            {
                TimeAttackManager.Instance.AddTimeBonus(linesClearedCount, isPerfect, currentCombo);
            }

            // [피버] 게이지 충전 — 타임어택에서는 피버 미사용
            if (!isFeverActive && GameModeManager.Current != GameMode.TimeAttack)
            {
                float charge = 0f;
                if (currentCombo > 1) charge += feverChargeCombo;
                if (linesClearedCount >= 2) charge += feverChargeMulti;
                if (isPerfect) charge += feverChargePerfect;

                if (charge > 0f)
                {
                    feverGauge = Mathf.Min(feverGauge + charge, feverMax);
                    UpdateFeverUI();
                    Debug.Log($"⚡ 피버 게이지 +{charge} (현재 {feverGauge:F0}/{feverMax})");

                    if (feverGauge >= feverMax) StartFever();
                }
            }

            Debug.Log($"{linesClearedCount}줄 클리어! {totalCells}칸 x {perCell} x 콤보{currentCombo}(x{comboMultiplier:F1})" +
                      $"{(isPerfect ? " x Perfect1.5" : "")}{(isFeverActive ? " x 🔥FEVER2.0" : "")} → +{earnedScore}점");

            // [올클리어] 줄을 지웠을 때만 발생 가능
            if (IsBoardEmpty())
            {
                int bonus = allClearBonus;
                if (isFeverActive) bonus = Mathf.RoundToInt(bonus * feverMultiplier);

                AddScore(bonus);
                ShowAllClear();

                Debug.Log($"🎉 ALL CLEAR! 보너스 +{bonus}점");
            }
        }
        else
        {
            // 줄을 하나도 못 지웠을 때만 콤보 초기화
            currentCombo = 0;
            if (comboRoutine != null) StopCoroutine(comboRoutine);
            if (comboText != null) comboText.gameObject.SetActive(false);
        }
    }

    public void AddScore(int points)
    {
        currentScore += points;
        if (scoreText != null) scoreText.text = currentScore.ToString("N0");
    }

    // 블록 파트에서 파편에 쓸 색을 뽑아냄
    private Color GetFragmentColor(GameObject blockPart)
    {
        if (blockPart == null) return Color.white;

        // 1순위: BlockInfo에 직접 지정된 색
        BlockInfo info = blockPart.GetComponentInParent<BlockInfo>();
        if (info != null && info.useCustomFragmentColor)
        {
            Color c = info.fragmentColor;
            c.a = 1f;
            return c;
        }

        // 2순위: SpriteRenderer에 색이 지정된 경우
        SpriteRenderer sr = blockPart.GetComponent<SpriteRenderer>();
        if (sr == null) sr = blockPart.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return Color.white;

        if (sr.color != Color.white) return sr.color;

        // 3순위: 스프라이트 이미지에 구워진 색
        return GetSpriteKeyColor(sr.sprite);
    }

    private Color GetSpriteKeyColor(Sprite sprite)
    {
        if (sprite == null) return Color.white;
        if (fragmentColorCache.TryGetValue(sprite, out Color cached)) return cached;

        Color result = Color.white;
        try
        {
            Rect r = sprite.textureRect;
            int px = Mathf.RoundToInt(r.x + r.width * 0.5f);
            int py = Mathf.RoundToInt(r.y + r.height * 0.5f);
            result = sprite.texture.GetPixel(px, py);
            result.a = 1f;
        }
        catch
        {
            Debug.LogWarning($"[Grid64] '{sprite.name}' 텍스처를 읽을 수 없습니다. " +
                             "Import Settings에서 Read/Write Enabled를 켜주세요.");
        }

        fragmentColorCache[sprite] = result;
        return result;
    }

    // =====================================================================
    //  연출 — 섬광 / 올클리어 / 흔들림
    // =====================================================================

    // 지워진 줄 위로 흰빛이 스쳐 지나가는 연출
    private IEnumerator LineFlash(bool isRow, int index)
    {
        GameObject flash = new GameObject("LineFlash");
        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();

        // Tile 프리팹의 스프라이트를 빌려 씀
        if (tilePrefab != null)
        {
            SpriteRenderer tileSR = tilePrefab.GetComponent<SpriteRenderer>();
            if (tileSR != null) sr.sprite = tileSR.sprite;
        }

        sr.sortingOrder = 50;   // 블록보다 앞에

        if (isRow)
        {
            flash.transform.position = new Vector3(0f, index - offsetY, 0f);
            flash.transform.localScale = new Vector3(width, 1f, 1f);
        }
        else
        {
            flash.transform.position = new Vector3(index - offsetX, 0f, 0f);
            flash.transform.localScale = new Vector3(1f, height, 1f);
        }

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(flashAlpha, 0f, elapsed / flashDuration);
            sr.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        Destroy(flash);
    }

    private void ShowAllClear()
    {
        if (explosionParticlePrefab != null)
        {
            StartCoroutine(AllClearParticles());
        }

        if (allClearText != null)
        {
            allClearText.text = Localization.Get("all_clear");
            allClearText.gameObject.SetActive(true);
            StartCoroutine(AllClearTextRoutine());
        }

        StartShake(0.45f, shakeMagnitudeAllClear);
        StartCoroutine(AllClearSound());
    }

    // 보드 전체에 대각선 물결로 파편
    private IEnumerator AllClearParticles()
    {
        for (int wave = 0; wave < width + height; wave++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = wave - x;
                if (y < 0 || y >= height) continue;

                Vector2 pos = GetWorldPosition(x, y);
                GameObject fx = Instantiate(explosionParticlePrefab, pos, Quaternion.identity);
                Destroy(fx, 2f);
            }
            yield return new WaitForSeconds(0.035f);
        }
    }

    // 음이 점점 올라가는 연타
    private IEnumerator AllClearSound()
    {
        for (int i = 0; i < 5; i++)
        {
            if (RetroAudio.Instance != null) RetroAudio.Instance.PlayClear(currentCombo + i + 2);
            yield return new WaitForSeconds(0.09f);
        }
    }

    private IEnumerator AllClearTextRoutine()
    {
        yield return PopScale(allClearText.transform, 0.3f, 1.35f, 0.25f, 0.15f);
        yield return new WaitForSeconds(1.2f);

        float fadeTime = 0.5f;
        float elapsed = 0f;
        Color original = allClearText.color;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            allClearText.color = new Color(original.r, original.g, original.b, alpha);
            yield return null;
        }

        allClearText.gameObject.SetActive(false);
        allClearText.color = new Color(original.r, original.g, original.b, 1f);
        allClearText.transform.localScale = Vector3.one;
    }

    // 작게 시작 → 크게 튀어오름 → 원래 크기로 착지
    private IEnumerator PopScale(Transform target, float startScale, float overshoot, float growTime, float settleTime)
    {
        float elapsed = 0f;
        while (elapsed < growTime)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(startScale, overshoot, elapsed / growTime);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < settleTime)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(overshoot, 1f, elapsed / settleTime);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    // 카메라 흔들림 시작 (중복 시 기존 것 중단)
    private void StartShake(float duration, float magnitude)
    {
        Transform cam = Camera.main.transform;

        if (shakeRoutine != null)
        {
            // 진행 중이던 흔들림을 끊고 원위치로 먼저 복귀
            StopCoroutine(shakeRoutine);
            cam.localPosition = camRestPos;
        }
        else
        {
            // 흔들림이 없는 상태에서만 원위치를 기록
            camRestPos = cam.localPosition;
        }

        shakeRoutine = StartCoroutine(CameraShake(duration, magnitude));
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        Transform cam = Camera.main.transform;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 게임이 멈춘 동안(일시정지·부활 제안)은 원위치에서 대기
            if (Time.timeScale == 0f)
            {
                cam.localPosition = camRestPos;
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;

            float damper = 1f - (elapsed / duration);
            float ox = Random.Range(-1f, 1f) * magnitude * damper;
            float oy = Random.Range(-1f, 1f) * magnitude * damper;

            // 항상 저장된 원위치 기준으로 흔듦 (누적 방지)
            cam.localPosition = new Vector3(camRestPos.x + ox, camRestPos.y + oy, camRestPos.z);
            yield return null;
        }

        cam.localPosition = camRestPos;
        shakeRoutine = null;
    }

    // =====================================================================
    //  피버타임
    // =====================================================================

    private void StartFever()
    {
        if (feverRoutine != null) StopCoroutine(feverRoutine);
        feverRoutine = StartCoroutine(FeverRoutine());
    }

    private IEnumerator FeverRoutine()
    {
        isFeverActive = true;
        Debug.Log($"🔥🔥🔥 FEVER TIME 시작! {feverDuration}초간 점수 {feverMultiplier}배!");

        // [발동 연출]
        StartShake(0.4f, shakeMagnitudeAllClear);
        StartCoroutine(FeverSound());
        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(feverPitch, 0.5f);

        if (feverText != null)
        {
            feverText.text = Localization.Get("fever_time");
            feverText.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < feverDuration)
        {
            elapsed += Time.deltaTime;
            feverGauge = Mathf.Lerp(feverMax, 0f, elapsed / feverDuration);
            UpdateFeverUI();

            // [지속 연출] 텍스트 두근거림 + 주황↔노랑
            if (feverText != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 8f) * 0.12f;
                feverText.transform.localScale = Vector3.one * pulse;

                float t = (Mathf.Sin(elapsed * 5f) + 1f) * 0.5f;
                feverText.color = Color.Lerp(feverTextBaseColor, Color.Lerp(feverTextBaseColor, Color.yellow, 0.7f), t);
            }

            // [지속 연출] 보드가 은은하게 물듦
            if (boardTintTarget != null)
            {
                float t = (Mathf.Sin(elapsed * 3f) + 1f) * 0.5f;
                boardTintTarget.color = Color.Lerp(boardOriginalColor,
                    boardOriginalColor * feverTintColor, 0.4f + t * 0.3f);
            }

            yield return null;
        }

        // [종료] 모든 효과 원복
        isFeverActive = false;
        feverGauge = 0f;
        UpdateFeverUI();

        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(1f, 0.8f);
        if (boardTintTarget != null) boardTintTarget.color = boardOriginalColor;

        if (feverText != null)
        {
            feverText.transform.localScale = Vector3.one;
            feverText.color = feverTextBaseColor;
            feverText.gameObject.SetActive(false);
        }

        Debug.Log("피버타임 종료");
    }

    // 음이 빠르게 상승하는 발동음
    private IEnumerator FeverSound()
    {
        for (int i = 0; i < 4; i++)
        {
            if (RetroAudio.Instance != null) RetroAudio.Instance.PlayClear(i + 5);
            yield return new WaitForSeconds(0.07f);
        }
    }

    private void UpdateFeverUI()
    {
        // 타임어택에서는 같은 바를 타이머가 사용
        if (GameModeManager.Current == GameMode.TimeAttack) return;

        if (feverGaugeBar != null)
            feverGaugeBar.fillAmount = feverGauge / feverMax;
    }

    // =====================================================================
    //  콤보 연출
    // =====================================================================

    private void ShowComboText(int combo)
    {
        if (comboText == null) return;

        comboText.text = Localization.Format("combo", combo);
        comboText.gameObject.SetActive(true);
        comboText.color = new Color(comboText.color.r, comboText.color.g, comboText.color.b, 1f);

        // 연속 콤보 시 이전 연출을 끊고 새로 시작
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        comboRoutine = StartCoroutine(ComboTextRoutine(combo));
    }

    private IEnumerator ComboTextRoutine(int combo)
    {
        // 콤보가 높을수록 더 크게 (최대 1.6배)
        float overshoot = Mathf.Min(1.25f + combo * 0.05f, 1.6f);
        yield return PopScale(comboText.transform, 0.5f, overshoot, 0.18f, 0.12f);

        yield return new WaitForSeconds(0.8f);

        float duration = 0.4f;
        float elapsed = 0f;
        Color original = comboText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            comboText.color = new Color(original.r, original.g, original.b, alpha);
            yield return null;
        }

        comboText.gameObject.SetActive(false);
        comboText.color = new Color(original.r, original.g, original.b, 1f);
        comboText.transform.localScale = Vector3.one;
    }

    // =====================================================================
    //  데드락 판정
    // =====================================================================

    // 덱에 남은 블록 중 하나라도 들어갈 자리가 있는지 검사
    public bool CheckGameOver(GameObject[] activeBlocks)
    {
        if (activeBlocks == null) return false;

        int checkCount = 0;

        foreach (GameObject block in activeBlocks)
        {
            if (block == null) continue;

            // 이미 배치된 블록은 제외
            BlockDrag dragComponent = block.GetComponent<BlockDrag>();
            if (dragComponent == null || dragComponent.enabled == false) continue;

            checkCount++;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (CanPlaceBlockAt(block, x, y)) return false;   // 생존
                }
            }
        }

        if (checkCount == 0) return false;

        // [타임어택] 시간 페널티 + 보드 구제
        if (TimeAttackManager.Instance != null && TimeAttackManager.Instance.IsRunning)
        {
            Debug.Log("💀 데드락 발생! (타임어택 - 구제 처리)");
            TimeAttackManager.Instance.OnDeadlock(GetPlayableBlocks(activeBlocks));
            return false;   // 구제 실패 시 TimeAttackManager가 게임오버 처리
        }

        Debug.Log("💀 데드락 발생! 더 이상 블록을 놓을 공간이 없습니다.");

        // [클래식] 한 판에 한 번, 광고가 준비됐으면 부활 제안
        if (CanOfferRevive())
        {
            OfferRevive(activeBlocks);
            return true;
        }

        ShowGameOver();
        return true;
    }

    // 특정 칸에 블록이 들어갈 수 있는지 가상 검사
    public bool CanPlaceBlockAt(GameObject block, int gridX, int gridY)
    {
        Vector2 testPos = GetWorldPosition(gridX, gridY);

        foreach (Transform child in block.transform)
        {
            if (child.name == "Shadow_Auto") continue;

            Vector2 childTestPos = testPos + (Vector2)child.localPosition;
            Vector2Int childGridIndex = GetGridIndex(childTestPos);

            if (!IsValidAndEmpty(childGridIndex.x, childGridIndex.y)) return false;
        }
        return true;
    }

    // =====================================================================
    //  스마트 스폰 지원 (DeckManager에서 호출)
    // =====================================================================

    public bool CanPlaceAnywhere(GameObject blockPrefab)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (CanPlaceBlockAt(blockPrefab, x, y)) return true;
        return false;
    }

    public bool CanCompleteLineWith(GameObject blockPrefab)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!CanPlaceBlockAt(blockPrefab, x, y)) continue;
                if (WouldCompleteLine(blockPrefab, x, y)) return true;
            }
        }
        return false;
    }

    // 가상 배치 후 라인 완성 여부
    private bool WouldCompleteLine(GameObject block, int gridX, int gridY)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2 testPos = GetWorldPosition(gridX, gridY);

        foreach (Transform child in block.transform)
        {
            if (child.name == "Shadow_Auto") continue;
            Vector2 childPos = testPos + (Vector2)child.localPosition;
            cells.Add(GetGridIndex(childPos));
        }

        int[,] temp = (int[,])gridData.Clone();
        foreach (var c in cells)
        {
            if (c.x < 0 || c.x >= width || c.y < 0 || c.y >= height) return false;
            temp[c.x, c.y] = 1;
        }

        foreach (var c in cells)
        {
            bool rowFull = true;
            for (int x = 0; x < width; x++)
                if (temp[x, c.y] == 0) { rowFull = false; break; }
            if (rowFull) return true;

            bool colFull = true;
            for (int y = 0; y < height; y++)
                if (temp[c.x, y] == 0) { colFull = false; break; }
            if (colFull) return true;
        }

        return false;
    }

    // =====================================================================
    //  데드락 구제 (타임어택 · 클래식 부활 공용)
    // =====================================================================

    // 구제 후보 하나 (가로 띠 또는 세로 띠)
    private struct RescueBand
    {
        public bool isRow;   // true = 가로줄 묶음, false = 세로줄 묶음
        public int start;    // 시작 인덱스
    }

    // 덱에서 아직 놓지 않은 블록만 골라냄
    private List<GameObject> GetPlayableBlocks(GameObject[] blocks)
    {
        List<GameObject> list = new List<GameObject>();
        if (blocks == null) return list;

        foreach (GameObject b in blocks)
        {
            if (b == null) continue;
            BlockDrag drag = b.GetComponent<BlockDrag>();
            if (drag == null || !drag.enabled) continue;
            list.Add(b);
        }
        return list;
    }

    // 가로·세로 후보 중 덱 블록이 들어갈 수 있는 곳을 골라 비움
    // 반환값: 비운 뒤 덱 블록이 하나라도 놓일 수 있으면 true
    public bool RescueBoard(int size, int minCells, List<GameObject> deckBlocks)
    {
        // 후보: 가로 3곳(하단/중단/상단) + 세로 3곳(좌/중/우)
        List<RescueBand> candidates = new List<RescueBand>();
        int[] rowStarts = { 0, height / 2 - 1, height - size };
        int[] colStarts = { 0, width / 2 - 1, width - size };
        foreach (int s in rowStarts) candidates.Add(new RescueBand { isRow = true,  start = s });
        foreach (int s in colStarts) candidates.Add(new RescueBand { isRow = false, start = s });

        List<RescueBand> fitting = new List<RescueBand>();     // 덱이 들어가는 후보
        List<RescueBand> preferred = new List<RescueBand>();   // + 충분히 비워지는 후보
        RescueBand fallback = candidates[0];
        int fallbackFilled = -1;

        foreach (RescueBand band in candidates)
        {
            int filled = CountFilledInBand(gridData, band, size);

            // 가장 많이 비워지는 후보를 최후의 수단으로 기억
            if (filled > fallbackFilled)
            {
                fallbackFilled = filled;
                fallback = band;
            }

            // 가상으로 비워보고 덱 블록이 들어가는지 확인
            int[,] temp = (int[,])gridData.Clone();
            ClearBandOnGrid(temp, band, size);

            if (AnyBlockFits(deckBlocks, temp))
            {
                fitting.Add(band);
                if (filled >= minCells) preferred.Add(band);
            }
        }

        RescueBand chosen;
        if (preferred.Count > 0)      chosen = preferred[Random.Range(0, preferred.Count)];
        else if (fitting.Count > 0)   chosen = fitting[Random.Range(0, fitting.Count)];
        else                          chosen = fallback;   // 이론상 거의 없음

        ExecuteRescue(chosen, size);

        Debug.Log($"🛟 보드 구제: {(chosen.isRow ? "가로" : "세로")} {chosen.start}번부터 {size}줄 비움");

        // 실제로 비운 뒤 다시 확인
        return AnyBlockFits(deckBlocks, gridData);
    }

    // 선택한 띠를 실제로 비우고 연출 재생
    private void ExecuteRescue(RescueBand band, int size)
    {
        // 라인 클리어보다 강한 흔들림 (정상 클리어와 구분)
        StartShake(0.5f, shakeMagnitudeAllClear);

        // 낮고 둔탁한 소리 (클리어 사운드와 구분)
        if (RetroAudio.Instance != null) RetroAudio.Instance.PlayPickup();

        int limit = band.isRow ? height : width;
        int span  = band.isRow ? width  : height;

        for (int i = band.start; i < band.start + size && i < limit; i++)
        {
            for (int j = 0; j < span; j++)
            {
                int x = band.isRow ? j : i;
                int y = band.isRow ? i : j;

                if (boardObjects[x, y] != null)
                {
                    if (explosionParticlePrefab != null)
                    {
                        GameObject fx = Instantiate(explosionParticlePrefab, GetWorldPosition(x, y), Quaternion.identity);

                        // 구제는 회색 파편 (정상 클리어의 블록 색과 구분)
                        ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
                        if (ps != null)
                        {
                            Color c = new Color(0.45f, 0.45f, 0.5f);
                            Color dim = new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, 1f);

                            var main = ps.main;
                            main.startColor = new ParticleSystem.MinMaxGradient(dim, c);
                        }

                        Destroy(fx, 2f);
                    }

                    Destroy(boardObjects[x, y]);
                    boardObjects[x, y] = null;
                }
                gridData[x, y] = 0;
            }
        }
    }

    // 띠 안에 채워진 칸 수
    private int CountFilledInBand(int[,] grid, RescueBand band, int size)
    {
        int count = 0;
        int limit = band.isRow ? height : width;
        int span  = band.isRow ? width  : height;

        for (int i = band.start; i < band.start + size && i < limit; i++)
        {
            for (int j = 0; j < span; j++)
            {
                int x = band.isRow ? j : i;
                int y = band.isRow ? i : j;
                if (grid[x, y] != 0) count++;
            }
        }
        return count;
    }

    // 가상 보드에서 띠를 비움 (실제 보드는 건드리지 않음)
    private void ClearBandOnGrid(int[,] grid, RescueBand band, int size)
    {
        int limit = band.isRow ? height : width;
        int span  = band.isRow ? width  : height;

        for (int i = band.start; i < band.start + size && i < limit; i++)
        {
            for (int j = 0; j < span; j++)
            {
                int x = band.isRow ? j : i;
                int y = band.isRow ? i : j;
                grid[x, y] = 0;
            }
        }
    }

    // 주어진 보드에 덱 블록 중 하나라도 놓을 수 있는지
    private bool AnyBlockFits(List<GameObject> blocks, int[,] grid)
    {
        if (blocks == null) return false;

        foreach (GameObject block in blocks)
        {
            if (block == null) continue;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (CanPlaceBlockOnGrid(block, x, y, grid)) return true;
        }
        return false;
    }

    // CanPlaceBlockAt과 같은 검사를 임의의 보드(가상 보드 포함)에 대해 수행
    private bool CanPlaceBlockOnGrid(GameObject block, int gridX, int gridY, int[,] grid)
    {
        Vector2 testPos = GetWorldPosition(gridX, gridY);

        foreach (Transform child in block.transform)
        {
            if (child.name == "Shadow_Auto") continue;

            Vector2 childPos = testPos + (Vector2)child.localPosition;
            Vector2Int idx = GetGridIndex(childPos);

            if (idx.x < 0 || idx.x >= width || idx.y < 0 || idx.y >= height) return false;
            if (grid[idx.x, idx.y] != 0) return false;
        }
        return true;
    }

    // =====================================================================
    //  [클래식] 광고 부활
    // =====================================================================

    private bool CanOfferRevive()
    {
        return !hasRevived
            && revivePanel != null
            && AdManager.Instance != null
            && AdManager.Instance.IsRewardedReady;   // 오프라인이면 제안 자체를 생략
    }

    private void OfferRevive(GameObject[] activeBlocks)
    {
        reviveDeck = GetPlayableBlocks(activeBlocks);

        Time.timeScale = 0f;   // 블록 조작 정지
        if (reviveAdButton != null) reviveAdButton.interactable = true;
        if (reviveDeclineButton != null) reviveDeclineButton.interactable = true;
        if (reviveScoreText != null) reviveScoreText.text = currentScore.ToString("N0");

        revivePanel.SetActive(true);

        StopReviveCountdown();
        reviveTimerRoutine = StartCoroutine(ReviveCountdown());
    }

    // 게임 시간이 멈춘 상태라 실제 시간(unscaled)으로 셈
    private IEnumerator ReviveCountdown()
    {
        float remaining = reviveCountdown;

        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(remaining / reviveCountdown);

            if (reviveCountdownFill != null) reviveCountdownFill.fillAmount = ratio;
            if (reviveCountdownText != null) reviveCountdownText.text = Mathf.CeilToInt(remaining).ToString();

            yield return null;
        }

        reviveTimerRoutine = null;

        // 시간이 다 되면 거절로 처리 (광고를 자동 재생하면 정책 위반)
        OnReviveDeclined();
    }

    private void StopReviveCountdown()
    {
        if (reviveTimerRoutine != null)
        {
            StopCoroutine(reviveTimerRoutine);
            reviveTimerRoutine = null;
        }
    }

    private void OnReviveAccepted()
    {
        StopReviveCountdown();   // 광고를 보는 동안에는 시간이 흐르지 않게
        if (reviveAdButton != null) reviveAdButton.interactable = false;       // 연타 방지
        if (reviveDeclineButton != null) reviveDeclineButton.interactable = false;

        AdManager.Instance.ShowRewarded(OnReviveRewarded, OnReviveFailed);
    }

    private void OnReviveDeclined()
    {
        if (revivePanel == null || !revivePanel.activeSelf) return;   // 중복 호출 방지

        StopReviveCountdown();
        revivePanel.SetActive(false);
        ShowGameOver();
    }

    // 광고를 끝까지 보고 돌아왔을 때
    private void OnReviveRewarded()
    {
        hasRevived = true;
        revivePanel.SetActive(false);
        Time.timeScale = 1f;

        // 타임어택 구제와 같은 로직 — 덱 블록이 들어가는 곳만 골라서 비움
        bool resolved = RescueBoard(reviveRescueSize, reviveMinCells, reviveDeck);

        if (!resolved) ShowGameOver();   // 이론상 없음 — 멈춘 채 두지 않음
    }

    // 광고를 중간에 닫았거나 표시에 실패했을 때
    private void OnReviveFailed()
    {
        revivePanel.SetActive(false);
        ShowGameOver();
    }

    // =====================================================================
    //  인게임 최고 기록
    // =====================================================================

    private void RefreshBestHud()
    {
        if (bestHud == null || bestHudText == null) return;

        // 첫 판에는 목표가 없으니 숨김
        bestHud.SetActive(bestScore > 0);
        bestHudText.text = bestScore.ToString("N0");
        lastHudScore = bestScore;
    }

    private void CheckRecordBreak()
    {
        if (bestHudText == null) return;

        // 기존 최고 기록을 처음 넘는 순간 (한 판에 한 번)
        if (!recordBroken && bestScore > 0 && currentScore > bestScore)
        {
            recordBroken = true;
            StartCoroutine(RecordBreakRoutine());

            // 높은 음으로 연타 (콤보 음높이 로직 재사용)
            if (RetroAudio.Instance != null) RetroAudio.Instance.PlayClear(8);
        }

        // 경신한 뒤로는 현재 점수를 따라 올라감
        if (recordBroken && currentScore != lastHudScore)
        {
            bestHudText.text = currentScore.ToString("N0");
            lastHudScore = currentScore;
        }
    }

    // "신기록!"이 튀어올랐다가 머문 뒤 사라지는 연출 (올 클리어와 같은 방식)
    private IEnumerator RecordBreakRoutine()
    {
        if (recordBreakText == null) yield break;

        recordBreakText.text = Localization.Get("new_record");
        recordBreakText.gameObject.SetActive(true);

        Color baseColor = recordBreakText.color;
        baseColor.a = 1f;
        recordBreakText.color = baseColor;

        // 작게 시작 → 크게 튀어오름 → 제 크기로 착지
        yield return PopScale(recordBreakText.transform, 0.6f, 1.2f, 0.2f, 0.15f);

        // 다 보인 채 유지
        yield return new WaitForSeconds(recordHoldTime);

        // 서서히 사라짐
        float elapsed = 0f;
        while (elapsed < recordFadeTime)
        {
            elapsed += Time.deltaTime;
            Color c = baseColor;
            c.a = Mathf.Lerp(1f, 0f, elapsed / recordFadeTime);
            recordBreakText.color = c;
            yield return null;
        }

        recordBreakText.gameObject.SetActive(false);
        recordBreakText.transform.localScale = Vector3.one;
        recordBreakText.color = baseColor;
    }

    // =====================================================================
    //  게임오버 / 씬 이동
    // =====================================================================

    public void ShowGameOver()
    {
        // 블록 조작 차단
        Time.timeScale = 0f;

        // 진행 중인 연출 코루틴 정리 (팝업 위로 메시지가 남는 것 방지)
        StopAllCoroutines();

        // 코루틴이 끊겨 종료 처리가 안 된 연출 강제 원복
        isFeverActive = false;
        feverRoutine = null;
        shakeRoutine = null;
        comboRoutine = null;
        reviveTimerRoutine = null;

        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(1f, 0.3f);
        if (boardTintTarget != null) boardTintTarget.color = boardOriginalColor;

        // 흔들림이 중단된 카메라를 원위치로
        CameraFitter fitter = Camera.main.GetComponent<CameraFitter>();
        if (fitter != null) fitter.Fit();

        // [타임어택] 타이머 정지
        if (TimeAttackManager.Instance != null)
        {
            TimeAttackManager.Instance.StopTimer();
        }

        gameOverPopup.SetActive(true);

        // 인게임 UI 숨기기
        if (feverGaugeBar != null) feverGaugeBar.transform.parent.gameObject.SetActive(false);
        if (feverText != null) feverText.gameObject.SetActive(false);
        if (allClearText != null) allClearText.gameObject.SetActive(false);
        if (comboText != null) comboText.gameObject.SetActive(false);
        if (recordBreakText != null) recordBreakText.gameObject.SetActive(false);

        // 최고 기록 갱신
        bool isNewRecord = currentScore > bestScore;
        if (isNewRecord)
        {
            bestScore = currentScore;
            GameModeManager.SetBestScore(GameModeManager.Current, bestScore);
        }

        if (finalScoreText != null)
            finalScoreText.text = currentScore.ToString("N0");

        if (bestScoreText != null)
            bestScoreText.text = Localization.Get("best") + "  " + bestScore.ToString("N0");

        if (newRecordBadge != null)
            newRecordBadge.SetActive(isNewRecord);
        
        // 기록 공유 — 매 게임오버마다 표시, 신기록이면 호박색으로 강조
        lastGameWasRecord = isNewRecord;
        if (shareButton != null)
        {
            shareButton.gameObject.SetActive(true);

            // 배경 없는 글자 버튼이라 아이콘과 글자색만 바꿈
            Color c = isNewRecord
                ? new Color(0.60f, 0.36f, 0f)       // #9A5B00 호박색
                : new Color(0.25f, 0.42f, 0.55f);   // #3F6A8C 차분한 남회색

            if (shareIcon != null) shareIcon.color = c;
            if (shareLabel != null) shareLabel.color = c;
        }

        CanvasGroup cg = gameOverPopup.GetComponent<CanvasGroup>();
        if (cg != null) StartCoroutine(FadeInPopup(cg));

        // 게임오버 카운트 (N회마다 전면광고) — 이번에 띄웠는지 결과를 받음
        bool adShown = AdManager.Instance != null && AdManager.Instance.OnGameOver();

        // 인앱 리뷰 — 신기록이면서 이번 판에 광고(전면·부활)를 보지 않았을 때만
        InAppReview.OnGameOver(isNewRecord, adShown || hasRevived);
    }

    // 신기록을 시스템 공유 창으로 전송 (카톡, 인스타, X 등)
    private void ShareRecord()
    {
        string mode = GameModeManager.GetDisplayName(GameModeManager.Current);
        string link = "https://jininh.github.io/grid64/";
        string key  = lastGameWasRecord ? "share_message" : "share_message_normal";
        string message = Localization.Format(key, mode, currentScore.ToString("N0"), link);

        ShareUtility.ShareText(message, Localization.Get("share_title"));
    }

    private IEnumerator FadeInPopup(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;   // timeScale 영향 안 받음
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(1f, 0f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(1f, 0f);

        SceneManager.LoadScene(mainMenuSceneName);
    }
}