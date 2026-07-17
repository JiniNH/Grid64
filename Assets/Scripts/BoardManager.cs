using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class BoardManager : MonoBehaviour
{
    // 싱글톤(Singleton): 외부 스크립트에서 쉽게 접근하도록 자기 자신을 저장
    public static BoardManager Instance; 

    [Header("UI 설정")]
    public TextMeshProUGUI scoreText;   // 에디터에서 연결할 텍스트 UI
    private int currentScore = 0;       // 현재 점수를 기억할 변수

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

        // 가로줄(row) 검사
        for (int y = 0; y < height; y++)
        {
            bool isRowFull = true;
            for (int x = 0; x< width; x++)
            {
                if (gridData[x, y] == 0) {isRowFull = false; break;}
            }
            if (isRowFull)
            {
                for (int x = 0; x < width; x++) linesToClear.Add(new Vector2Int(x, y));
            }
        }

        // 세로줄(column) 검사
        for (int x = 0; x < width; x++)
        {
            bool isColFull = true;
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] == 0) {isColFull = false; break;}
            }
            if (isColFull)
            {
                for (int y = 0; y < height; y++) linesToClear.Add(new Vector2Int(x, y));
            }
        }

        // 꽉 찬 줄의 오브젝트 파괴 및 배열 초기화
        foreach (Vector2Int pos in linesToClear)
        {
            if (boardObjects[pos.x, pos.y] != null)
            {
                // 실제 화면에서 블록 조각을 파괴
                Destroy(boardObjects[pos.x, pos.y]);
                AddScore(100);
                boardObjects[pos.x, pos.y] = null;
            }
            // 데이터를 다시 빈칸(0)으로 되돌림
            gridData[pos.x, pos.y] = 0;
        }

        if (linesToClear.Count > 0)
        {
            Debug.Log("라인 클리어 성공!");
        }
    }

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

        // 5. 모든 블록을 모든 칸에 대입해 보았으나 단 하나도 들어갈 수 없을 때
        Debug.Log("💀 데드락 발생! 더 이상 블록을 놓을 공간이 없습니다. (게임 오버)");
        return true; 
    }

    // 특정 그리드 좌표에 블록이 들어갈 수 있는지 수학적으로 가상 검사
    private bool CanPlaceBlockAt(GameObject block, int gridX, int gridY)
    {
        // 가상의 기준 좌표 설정
        Vector2 testPos = GetWorldPosition(gridX, gridY);

        foreach (Transform child in block.transform)
        {
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
}