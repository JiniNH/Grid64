using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class BoardManager : MonoBehaviour
{
    // 싱글톤(Singleton): 외부 스크립트에서 쉽게 접근하도록 자기 자신을 저장
    public static BoardManager Instance;

    [Header("UI 설정")]
    public TextMeshProUGUI scoreText;       // 에디터에서 연결할 텍스트 UI
    public GameObject gameOverPopup;        // 팝업 패널 연결용
    public TextMeshProUGUI finalScoreText;  // 게임오버 팝업의 최종 점수 텍스트
    private int currentScore = 0;           // 현재 점수를 기억할 변수

    [Header("이펙트 설정")]
    public GameObject explosionParticlePrefab;  // 파티클 프리팹 연결

    [Header("콤보 설정")]
    public int currentCombo = 0;        // 현재 콤보 횟수
    public TextMeshProUGUI comboText;   // 콤보 글자를 띄워줄 UI

    private Coroutine comboRoutine;     // 실행 중인 콤보 연출 추적용

    [Header("피버타임 설정")]
    public float feverGauge = 0f;           // 현재 게이지 (0~100)
    public float feverMax = 100f;           // 최대치
    public float feverChargeCombo = 15f;    // 2콤보 이상 유지 시
    public float feverChargeMulti = 20f;    // 다중 클리어(2줄 이상) 시
    public float feverChargePerfect = 25f;  // Perfect(교차) 시
    public float feverDuration = 10f;       // 피버 지속 시간(초)
    public float feverMultiplier = 2f;      // 피버 중 점수 배수

    private bool isFeverActive = false;     // 피버 진행 중인지
    private Coroutine feverRoutine;         // 피버 타이머 추적

    [Header("피버 UI (선택)")]
    public Image feverGaugeBar;             // 게이지 바 (Fill 방식)
    public TextMeshProUGUI feverText;       // "FEVER TIME!" 표시용

    [Header("피버 연출")]
    public SpriteRenderer boardTintTarget;  // 피버 중 물들일 보드 배경 (선택)
    public Color feverTintColor = new Color(1f, 0.92f, 0.85f);  // 보드에 덧입힐 색
    public float feverPitch = 1.08f;        // 피버 중 BGM 재생 속도

    private Color boardOriginalColor;
    private Color feverTextBaseColor;

    [Header("게임오버 UI")]
    public TextMeshProUGUI bestScoreText;   // 최고 기록
    public GameObject newRecordBadge;       // NEW RECORD! 표시
    public TextMeshProUGUI modeLabelText;   // 현재 모드 표시 (선택)
    public string mainMenuSceneName = "MainMenu";

    [Header("올클리어 보너스")]
    public int allClearBonus = 3000;        // 올클리어 시 보너스 점수
    public TextMeshProUGUI allClearText;    // "ALL CLEAR!" 표시용

    [Header("카메라 흔들림")]
    public float shakeMagnitudeClear = 0.08f;     // 일반 라인 클리어 시
    public float shakeMagnitudeCombo = 0.15f;     // 다중/콤보 클리어 시
    public float shakeMagnitudeAllClear = 0.25f;  // 올클리어 시
    private Vector3 camRestPos;                   // 흔들림 기준이 되는 카메라 원위치

    [Header("라인 섬광 효과")]
    public float flashDuration = 0.1f;               // 섬광 지속 시간
    [Range(0f, 1f)] public float flashAlpha = 0.22f; // 섬광 최대 밝기

    private Coroutine shakeRoutine;         // 흔들림 중복 방지

    private int bestScore = 0;

    public int width = 8;
    public int height = 8;
    public GameObject tilePrefab;

    [Header("보드 위치")]
    [Tooltip("보드를 위아래로 미세 조정 (양수 = 위로)")]
    public float boardYOffset = 0f;

    [Header("나가기 확인 팝업")]
    public GameObject exitPopup;        // ExitDimPanel 연결

    // 외부에서 읽고 쓸 수 있도록 public으로 변경
    public int[,] gridData;

    // 화면에 생성된 실제 블록 파트들을 저장할 배열
    public GameObject[,] boardObjects;

    // 좌표 변환에 쓰일 오프셋을 전역 변수로 분리
    private float offsetX, offsetY;

    void Awake()
    {
        // 게임 시작 시 싱글톤 인스턴스 초기화
        Instance = this;
    }

    void Start()
    {
        InitializeBoard();
        UpdateFeverUI();

        // 현재 모드의 최고 기록을 불러옴
        bestScore = GameModeManager.GetBestScore(GameModeManager.Current);

        if (modeLabelText != null)
            modeLabelText.text = GameModeManager.GetDisplayName(GameModeManager.Current);
        if (boardTintTarget != null) boardOriginalColor = boardTintTarget.color;
        if (feverText != null) feverTextBaseColor = feverText.color;
    }

    void InitializeBoard()
    {
        gridData = new int[width, height];

        // 배열 초기화
        boardObjects = new GameObject[width, height];

        offsetX = (width - 1) / 2f;
        offsetY = (height - 1) / 2f - boardYOffset;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridData[x, y] = 0; // 0은 빈칸
                Vector2 position = new Vector2(x - offsetX, y - offsetY);
                Instantiate(tilePrefab, position, Quaternion.identity);
            }
        }
    }

    // 월드 좌표(실수)를 배열 인덱스(정수)로 변환하는 함수
    public Vector2Int GetGridIndex(Vector2 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + offsetX);
        int y = Mathf.RoundToInt(worldPos.y + offsetY);
        return new Vector2Int(x, y);
    }

    // 배열 인덱스(정수)를 다시 월드 좌표(실수)로 변환하는 함수 (스냅용)
    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(x - offsetX, y - offsetY);
    }

    // 해당 인덱스가 보드판 안쪽에 있고, 빈칸(0)인지 검증하는 함수
    public bool IsValidAndEmpty(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return gridData[x, y] == 0;
        }
        return false; // 보드 밖이거나 이미 채워져 있으면 false
    }

    // 보드가 완전히 비었는지 검사
    private bool IsBoardEmpty()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] != 0) return false;
            }
        }
        return true;
    }

    // [타임어택] 데드락 구제 — 상/중/하단 중 무작위 위치의 줄을 비움
    public void RescueBoard(int rows, int minCells)
    {
        // 후보 시작 y좌표 (하단 / 중단 / 상단)
        int[] candidates = { 0, height / 2 - 1, height - rows };
        int startY = candidates[Random.Range(0, candidates.Length)];

        // 안전장치: 고른 위치가 너무 비어 있으면 하단으로 대체
        if (CountFilledInRows(startY, rows) < minCells)
        {
            startY = 0;
        }

        // 라인 클리어보다 강한 흔들림 (정상 클리어와 구분)
        StartShake(0.5f, shakeMagnitudeAllClear);

        // 낮고 둔탁한 소리 (클리어 사운드와 구분)
        RetroAudio.Instance.PlayPickup();

        for (int y = startY; y < startY + rows && y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (boardObjects[x, y] != null)
                {
                    Vector2 effectPos = GetWorldPosition(x, y);

                    if (explosionParticlePrefab != null)
                    {
                        GameObject fx = Instantiate(explosionParticlePrefab, effectPos, Quaternion.identity);

                        ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
                        if (ps != null)
                        {
                            // 구제는 회색 파편 (정상 클리어의 블록 색과 구분)
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

        Debug.Log($"🛟 보드 구제: y={startY}부터 {rows}줄 비움");
    }

    // 특정 줄 범위에 채워진 칸이 몇 개인지 세기
    private int CountFilledInRows(int startY, int rows)
    {
        int count = 0;
        for (int y = startY; y < startY + rows && y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (gridData[x, y] != 0) count++;
            }
        }
        return count;
    }

    // 빙고(라인 클리어)를 검사하고 삭제하는 핵심 함수
    public void CheckAndClearLines()
    {
        List<Vector2Int> linesToClear = new List<Vector2Int>();

        int rowCleared = 0;   // 지운 가로줄 수
        int colCleared = 0;   // 지운 세로줄 수

        // 가로줄(row) 검사
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
                StartCoroutine(LineFlash(true, y));      // ← 추가
            }
        }

        // 세로줄(column) 검사
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
                StartCoroutine(LineFlash(false, x));     // ← 추가
            }
        }

        int linesClearedCount = rowCleared + colCleared;  // 총 지운 줄 수

        // 꽉 찬 줄의 오브젝트 파괴 및 배열 초기화
        foreach (Vector2Int pos in linesToClear)
        {
            if (boardObjects[pos.x, pos.y] != null)
            {
                // 이펙트가 터질 월드 좌표 계산
                Vector2 effectPos = GetWorldPosition(pos.x, pos.y);

                // 해당 위치에 파티클 프리팹을 생성(복제)
                if (explosionParticlePrefab != null)
                {
                    GameObject fx = Instantiate(explosionParticlePrefab, effectPos, Quaternion.identity);

                    // 지워지는 블록의 색을 파티클에 적용
                    ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
                    if (ps != null)
                    {
                        Color c = GetFragmentColor(boardObjects[pos.x, pos.y]);
                        Color dim = new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, 1f);

                        var main = ps.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(dim, c);
                    }

                    Destroy(fx, 2f);   // 파티클 오브젝트 누수 방지
                }

                // 실제 화면에서 블록 조각을 파괴
                Destroy(boardObjects[pos.x, pos.y]);
                boardObjects[pos.x, pos.y] = null;
            }
            // 데이터를 다시 빈칸(0)으로 되돌림
            gridData[pos.x, pos.y] = 0;
        }

        if (linesClearedCount > 0)
        {
            currentCombo++;                                 // 줄을 지웠으니 콤보 증가
            RetroAudio.Instance.PlayClear(currentCombo);    // 클리어 사운드 (콤보에 따라 음 상승)

            // 줄당 보너스 점수 (동시에 많이 지울수록 칸당 가치 상승)
            int perCell = 0;
            if (linesClearedCount == 1) perCell = 100;
            else if (linesClearedCount == 2) perCell = 150;
            else perCell = 200;                             // 3줄 이상

            // 지운 총 칸 수 (지운 줄 수 x 8칸)
            int totalCells = linesClearedCount * width;

            // 콤보 배수
            float comboMultiplier = 1f + 0.1f * (currentCombo - 1);

            // 최종 = 총칸수 x 칸당점수 x 콤보배수
            int earnedScore = Mathf.RoundToInt(totalCells * perCell * comboMultiplier);

            // [Perfect] 가로+세로 동시 클리어 = 교차 클리어 보너스
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

            // [연출] 상황에 따라 화면 흔들림 강도 차등 적용
            bool isBigClear = (linesClearedCount >= 2 || currentCombo > 1 || isPerfect);
            StartShake(0.25f, isBigClear ? shakeMagnitudeCombo : shakeMagnitudeClear);

            if (currentCombo > 1)
            {
                ShowComboText(currentCombo); // 콤보 텍스트 띄우기
            }

            AddScore(earnedScore);
            
            // [타임어택] 클리어 종류에 따라 시간 보너스
            if (TimeAttackManager.Instance != null)
            {
                TimeAttackManager.Instance.AddTimeBonus(linesClearedCount, isPerfect);
            }

            // [피버] 게이지 충전 — 타임어택에서는 피버 미사용
            if (!isFeverActive && GameModeManager.Current != GameMode.TimeAttack)
            {
                float charge = 0f;

                if (currentCombo > 1) charge += feverChargeCombo;        // 콤보 유지
                if (linesClearedCount >= 2) charge += feverChargeMulti;  // 다중 클리어
                if (isPerfect) charge += feverChargePerfect;             // 교차 Perfect

                if (charge > 0f)
                {
                    feverGauge = Mathf.Min(feverGauge + charge, feverMax);
                    UpdateFeverUI();
                    Debug.Log($"⚡ 피버 게이지 +{charge} (현재 {feverGauge:F0}/{feverMax})");

                    if (feverGauge >= feverMax)
                    {
                        StartFever();
                    }
                }
            }

            Debug.Log($"{linesClearedCount}줄 클리어! {totalCells}칸 x {perCell} x 콤보{currentCombo}(x{comboMultiplier:F1})" +
                      $"{(isPerfect ? " x Perfect1.5" : "")}{(isFeverActive ? " x 🔥FEVER2.0" : "")} → +{earnedScore}점");

            // [올클리어] 보드를 완전히 비웠는지 검사 (줄을 지웠을 때만 발생 가능)
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
            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }

    // 스프라이트별 대표색 캐시 (매 프레임 텍스처를 읽지 않도록)
    private Dictionary<Sprite, Color> fragmentColorCache = new Dictionary<Sprite, Color>();

    // 블록 파트에서 파편에 쓸 색을 뽑아냄
    private Color GetFragmentColor(GameObject blockPart)
    {
        if (blockPart == null) return Color.white;

        // 1순위: 부모 블록의 BlockInfo에 직접 지정된 색
        BlockInfo info = blockPart.GetComponentInParent<BlockInfo>();
        if (info != null && info.useCustomFragmentColor)
        {
            Color c = info.fragmentColor;
            c.a = 1f;
            return c;
        }

        // 2순위: SpriteRenderer에 색이 직접 지정된 경우
        SpriteRenderer sr = blockPart.GetComponent<SpriteRenderer>();
        if (sr == null) sr = blockPart.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return Color.white;

        if (sr.color != Color.white) return sr.color;

        // 3순위: 스프라이트 이미지에 색이 구워진 경우 텍스처에서 추출
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

    // 지워진 줄 위로 흰빛이 스쳐 지나가는 연출
    private IEnumerator LineFlash(bool isRow, int index)
    {
        GameObject flash = new GameObject("LineFlash");
        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();

        // Tile 프리팹의 스프라이트를 빌려 씀 (단순 사각형)
        if (tilePrefab != null)
        {
            SpriteRenderer tileSR = tilePrefab.GetComponent<SpriteRenderer>();
            if (tileSR != null) sr.sprite = tileSR.sprite;
        }

        sr.sortingOrder = 50;   // 블록보다 앞에

        // 가로줄이면 가로로 길게, 세로줄이면 세로로 길게
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

        // 최대 밝기에서 시작해 곧장 사라짐 (페이드 인 없음)
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

    // ===== 올클리어 연출 =====

    // 올클리어 연출 (텍스트 + 보드 전체 파티클 + 흔들림 + 사운드)
    private void ShowAllClear()
    {
        // 보드 전체에 파티클 터뜨리기
        if (explosionParticlePrefab != null)
        {
            StartCoroutine(AllClearParticles());
        }

        // 텍스트 표시
        if (allClearText != null)
        {
            allClearText.text = "ALL CLEAR!";
            allClearText.gameObject.SetActive(true);
            StartCoroutine(AllClearTextRoutine());
        }

        // 강한 화면 흔들림
        StartShake(0.45f, shakeMagnitudeAllClear);

        // 화려한 사운드 연타
        StartCoroutine(AllClearSound());
    }

    // 보드 전체에 대각선 물결로 파티클 생성
    private IEnumerator AllClearParticles()
    {
        // 대각선 순서로 퍼지게 (좌하단 → 우상단)
        for (int wave = 0; wave < width + height; wave++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = wave - x;
                if (y < 0 || y >= height) continue;

                Vector2 pos = GetWorldPosition(x, y);
                GameObject fx = Instantiate(explosionParticlePrefab, pos, Quaternion.identity);
                Destroy(fx, 2f);   // 파티클 오브젝트 누수 방지
            }
            yield return new WaitForSeconds(0.035f);
        }
    }

    // 화려한 사운드 연타 (음이 점점 올라감)
    private IEnumerator AllClearSound()
    {
        for (int i = 0; i < 5; i++)
        {
            RetroAudio.Instance.PlayClear(currentCombo + i + 2);
            yield return new WaitForSeconds(0.09f);
        }
    }

    // 피버 발동 사운드 (음이 빠르게 상승)
    private IEnumerator FeverSound()
    {
        for (int i = 0; i < 4; i++)
        {
            RetroAudio.Instance.PlayClear(i + 5);
            yield return new WaitForSeconds(0.07f);
        }
    }

    private IEnumerator AllClearTextRoutine()
    {
        yield return PopScale(allClearText.transform, 0.3f, 1.35f, 0.25f, 0.15f);

        yield return new WaitForSeconds(1.2f);

        // 페이드 아웃
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

    // ===== 공통 연출 유틸 =====

    // 작게 시작 → 크게 튀어오름 → 원래 크기로 착지 (오버슈트 팝)
    private IEnumerator PopScale(Transform target, float startScale, float overshoot, float growTime, float settleTime)
    {
        float elapsed = 0f;
        while (elapsed < growTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(startScale, overshoot, elapsed / growTime);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < settleTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(overshoot, 1f, elapsed / settleTime);
            target.localScale = Vector3.one * scale;
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
            // 진행 중이던 흔들림을 끊고, 저장해둔 원위치로 먼저 되돌림
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
            elapsed += Time.deltaTime;

            // 시간이 지날수록 흔들림이 잦아들게
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

    // ===== 피버타임 =====

    // 피버타임 시작
    private void StartFever()
    {
        if (feverRoutine != null) StopCoroutine(feverRoutine);
        feverRoutine = StartCoroutine(FeverRoutine());
    }

    private IEnumerator FeverRoutine()
    {
        isFeverActive = true;
        Debug.Log($"🔥🔥🔥 FEVER TIME 시작! {feverDuration}초간 점수 {feverMultiplier}배!");

        // [발동 연출] 카메라 흔들림 + 상승 사운드
        StartShake(0.4f, shakeMagnitudeAllClear);
        StartCoroutine(FeverSound());
        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(feverPitch, 0.5f);

        if (feverText != null)
        {
            feverText.text = "FEVER TIME!";
            feverText.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < feverDuration)
        {
            elapsed += Time.deltaTime;
            feverGauge = Mathf.Lerp(feverMax, 0f, elapsed / feverDuration);
            UpdateFeverUI();

            // [지속 연출] 텍스트 두근거림 + 색상 순환
            if (feverText != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 8f) * 0.12f;
                feverText.transform.localScale = Vector3.one * pulse;

                // 주황 ↔ 노랑 왕복
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

    // 게이지 UI 갱신
    private void UpdateFeverUI()
    {
        // 타임어택에서는 같은 바를 타이머가 사용하므로 건드리지 않음
        if (GameModeManager.Current == GameMode.TimeAttack) return;

        if (feverGaugeBar != null)
        {
            feverGaugeBar.fillAmount = feverGauge / feverMax;
        }
    }

    // ===== 콤보 연출 =====

    // 콤보 텍스트를 팝업처럼 띄웠다가 서서히 사라지게 하는 함수
    private void ShowComboText(int combo)
    {
        if (comboText == null) return;

        comboText.text = combo + " COMBO!";
        comboText.gameObject.SetActive(true);
        comboText.color = new Color(comboText.color.r, comboText.color.g, comboText.color.b, 1f);

        // 이미 실행 중인 콤보 연출이 있으면 멈추고 새로 시작 (연속 콤보 시 겹침 방지)
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        comboRoutine = StartCoroutine(ComboTextRoutine(combo));
    }

    private IEnumerator ComboTextRoutine(int combo)
    {
        // 콤보가 높을수록 더 크게 튀어오름 (최대 1.6배)
        float overshoot = Mathf.Min(1.25f + combo * 0.05f, 1.6f);
        yield return PopScale(comboText.transform, 0.5f, overshoot, 0.18f, 0.12f);

        // 잠깐 유지
        yield return new WaitForSeconds(0.8f);

        // 페이드 아웃
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

    // ===== 게임오버 판정 =====

    // 덱에 남은 블록들이 하나라도 들어갈 자리가 있는지 전체 검사
    public bool CheckGameOver(GameObject[] activeBlocks)
    {
        // 배열 자체가 비어있으면 검사 패스
        if (activeBlocks == null) return false;

        int checkCount = 0; // 유효한 대기 블록 개수 확인용

        foreach (GameObject block in activeBlocks)
        {
            // 파괴되었거나 텅 빈 슬롯은 패스
            if (block == null) continue;

            // 이미 보드판에 배치 완료되어 드래그 기능이 꺼진 블록은 검사에서 제외
            BlockDrag dragComponent = block.GetComponent<BlockDrag>();
            if (dragComponent == null || dragComponent.enabled == false) continue;

            checkCount++; // 검사할 유효 블록 카운트 증가

            // (0,0)부터 (7,7)까지 모든 칸에 이 블록을 놓아보는 시뮬레이션
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (CanPlaceBlockAt(block, x, y))
                    {
                        // 단 하나라도 놓을 수 있는 자리를 발견하면 생존 판정 (게임 계속)
                        return false;
                    }
                }
            }
        }

        // 검사할 남은 덱 블록이 0개였다면 데드락 상황이 아님
        if (checkCount == 0) return false;

        // [타임어택] 게임 오버 대신 시간 페널티 + 보드 구제
        if (TimeAttackManager.Instance != null && TimeAttackManager.Instance.IsRunning)
        {
            Debug.Log("💀 데드락 발생! (타임어택 - 구제 처리)");
            TimeAttackManager.Instance.OnDeadlock();
            return false;   // 게임은 계속됨
        }

        Debug.Log("💀 데드락 발생! 더 이상 블록을 놓을 공간이 없습니다. (게임 오버)");

        // 팝업 호출
        ShowGameOver();
        return true;
    }

    // 특정 그리드 좌표에 블록이 들어갈 수 있는지 수학적으로 가상 검사
    public bool CanPlaceBlockAt(GameObject block, int gridX, int gridY)
    {
        // 가상의 기준 좌표 설정
        Vector2 testPos = GetWorldPosition(gridX, gridY);

        foreach (Transform child in block.transform)
        {
            // 자동 생성된 그림자는 검사에서 제외
            if (child.name == "Shadow_Auto") continue;

            // 자식 파트의 로컬 위치를 가상 부모 좌표에 더해 예상 월드 좌표를 구함
            Vector2 childTestPos = testPos + (Vector2)child.localPosition;
            Vector2Int childGridIndex = GetGridIndex(childTestPos);

            // 해당 칸이 보드 밖이거나 이미 채워져 있다면 실패
            if (!IsValidAndEmpty(childGridIndex.x, childGridIndex.y))
            {
                return false;
            }
        }
        // 모든 자식 파트가 무사히 빈칸에 들어간다면 성공
        return true;
    }

    // ===== 스마트 스폰 지원 (DeckManager에서 호출) =====

    // 해당 블록을 보드 어딘가에 놓을 수 있는지 검사
    public bool CanPlaceAnywhere(GameObject blockPrefab)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (CanPlaceBlockAt(blockPrefab, x, y)) return true;
            }
        }
        return false;
    }

    // 해당 블록을 어딘가에 놓았을 때 라인을 완성할 수 있는지 검사
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

    // 가상 배치 후 라인 완성 여부 판단
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

        // 가상 보드 만들기 (원본 복사)
        int[,] temp = (int[,])gridData.Clone();
        foreach (var c in cells)
        {
            if (c.x < 0 || c.x >= width || c.y < 0 || c.y >= height) return false;
            temp[c.x, c.y] = 1;
        }

        // 가상 보드에서 라인이 완성됐는지 확인
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

    // ===== 점수 / 게임오버 UI =====

    // 점수를 획득하고 UI 텍스트를 즉시 갱신하는 함수
    public void AddScore(int points)
    {
        currentScore += points;     // 점수 누적

        // 텍스트 UI가 잘 연결되어 있을 때만 글자 변경
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("N0");
        }
    }

    // 게임 오버 팝업 띄우기 함수 (데드락 판정 시 호출)
    public void ShowGameOver()
    {
        // 게임오버 후 블록 조작 차단
        Time.timeScale = 0f;

        // 진행 중인 연출 코루틴 정리 (팝업 위로 메시지가 남는 것 방지)
        StopAllCoroutines();

        // 피버 연출 강제 원복 (코루틴이 끊겨 종료 처리가 안 되므로)
        isFeverActive = false;
        feverRoutine = null;
        shakeRoutine = null;
        comboRoutine = null;

        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(1f, 0.3f);
        if (boardTintTarget != null) boardTintTarget.color = boardOriginalColor;

        // 흔들림이 중단된 카메라를 원위치로 복구
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
            bestScoreText.text = "BEST  " + bestScore.ToString("N0");

        if (newRecordBadge != null)
            newRecordBadge.SetActive(isNewRecord);

        CanvasGroup cg = gameOverPopup.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            StartCoroutine(FadeInPopup(cg));
        }

        // 게임오버 카운트 (N회마다 전면광고)
        if (AdManager.Instance != null) AdManager.Instance.OnGameOver();
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
        // 게임 오버 시 멈춘 시간 복구
        Time.timeScale = 1f;
        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(1f, 0f);

        // 현재 열려있는 씬의 이름을 가져와서 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 메인 메뉴로 돌아가기 (모드 전환용)
    public void GoToMenu()
    {
        Time.timeScale = 1f;   // 타임어택에서 일시정지를 쓸 경우 대비
        if (BGMPlayer.Instance != null) BGMPlayer.Instance.SetPitch(1f, 0f);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // 나가기 버튼 → 확인 팝업 표시
    public void ShowExitConfirm()
    {
        if (exitPopup == null) return;

        Time.timeScale = 0f;            // 진행 정지 (타임어택 타이머 포함)
        exitPopup.SetActive(true);
    }

    // 계속하기 → 팝업 닫고 게임 재개
    public void CloseExitConfirm()
    {
        if (exitPopup != null) exitPopup.SetActive(false);
        Time.timeScale = 1f;
    }
}