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

    public int width = 8;
    public int height = 8;
    public GameObject tilePrefab;

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
    }

    void InitializeBoard()
    {
        gridData = new int[width, height];

        // 배열 초기화
        boardObjects = new GameObject[width, height];

        offsetX = (width - 1) / 2f;
        offsetY = (height - 1) / 2f;

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
                    Instantiate(explosionParticlePrefab, effectPos, Quaternion.identity);
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

            if (currentCombo > 1)
            {
                ShowComboText(currentCombo); // 콤보 텍스트 띄우기
            }

            AddScore(earnedScore);

            // [피버] 게이지 충전 — 테크니컬 클리어에만 반응 (기본 클리어는 충전 없음)
            if (!isFeverActive)
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

        // FEVER 텍스트 표시
        if (feverText != null)
        {
            feverText.text = "FEVER TIME!";
            feverText.gameObject.SetActive(true);
        }

        // 지속 시간 동안 게이지를 서서히 감소시켜 남은 시간을 시각화
        float elapsed = 0f;
        while (elapsed < feverDuration)
        {
            elapsed += Time.deltaTime;
            feverGauge = Mathf.Lerp(feverMax, 0f, elapsed / feverDuration);
            UpdateFeverUI();
            yield return null;
        }

        // 종료 처리
        isFeverActive = false;
        feverGauge = 0f;
        UpdateFeverUI();

        if (feverText != null)
        {
            feverText.gameObject.SetActive(false);
        }

        Debug.Log("피버타임 종료");
    }

    // 게이지 UI 갱신
    private void UpdateFeverUI()
    {
        if (feverGaugeBar != null)
        {
            feverGaugeBar.fillAmount = feverGauge / feverMax;
        }
    }

    // ===== 콤보 연출 =====

    // 콤보 텍스트를 잠깐 띄웠다가 서서히 사라지게 하는 함수
    private void ShowComboText(int combo)
    {
        if (comboText == null) return;

        comboText.text = combo + " COMBO!";
        comboText.gameObject.SetActive(true);

        // 이미 실행 중인 콤보 연출이 있으면 멈추고 새로 시작 (연속 콤보 시 겹침 방지)
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        comboRoutine = StartCoroutine(ComboTextRoutine());
    }

    private IEnumerator ComboTextRoutine()
    {
        // 잠깐 그대로 보여주기 (1초)
        yield return new WaitForSeconds(1f);

        // 0.5초에 걸쳐 서서히 사라지기
        float duration = 0.5f;
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
        comboText.color = new Color(original.r, original.g, original.b, 1f); // 다음을 위해 alpha 복구
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

        // 모든 블록을 모든 칸에 대입해 보았으나 단 하나도 들어갈 수 없을 때
        Debug.Log("💀 데드락 발생! 더 이상 블록을 놓을 공간이 없습니다. (게임 오버)");

        // 팝업 호출
        ShowGameOver();
        return true;
    }

    // 특정 그리드 좌표에 블록이 들어갈 수 있는지 수학적으로 가상 검사
    private bool CanPlaceBlockAt(GameObject block, int gridX, int gridY)
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

    // ===== 점수 / 게임오버 UI =====

    // 점수를 획득하고 UI 텍스트를 즉시 갱신하는 함수
    public void AddScore(int points)
    {
        currentScore += points;     // 점수 누적

        // 텍스트 UI가 잘 연결되어 있을 때만 글자 변경
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore.ToString();
        }
    }

    // 게임 오버 팝업 띄우기 함수 (데드락 판정 시 호출)
    public void ShowGameOver()
    {
        gameOverPopup.SetActive(true);  // 숨겨뒀던 팝업 패널을 다시 켜기

        // 피버 UI 숨기기
        if (feverGaugeBar != null) feverGaugeBar.transform.parent.gameObject.SetActive(false);
        if (feverText != null) feverText.gameObject.SetActive(false);

        // 최종 점수 표시
        if (finalScoreText != null)
        {
            finalScoreText.text = "Final Score\n" + currentScore.ToString();
        }

        CanvasGroup cg = gameOverPopup.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            StartCoroutine(FadeInPopup(cg));
        }
    }

    private IEnumerator FadeInPopup(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    public void RestartGame()
    {
        // 현재 열려있는 씬의 이름을 가져와서 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}