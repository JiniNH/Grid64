using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    // 싱글톤(Singleton): 외부 스크립트에서 쉽게 접근하도록 자기 자신을 저장
    public static BoardManager Instance; 

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

    // 1. 월드 좌표(실수)를 배열 인덱스(정수)로 변환하는 함수
    public Vector2Int GetGridIndex(Vector2 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + offsetX);
        int y = Mathf.RoundToInt(worldPos.y + offsetY);
        return new Vector2Int(x, y);
    }

    // 2. 배열 인덱스(정수)를 다시 월드 좌표(실수)로 변환하는 함수 (스냅용)
    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(x - offsetX, y - offsetY);
    }

    // 3. 해당 인덱스가 보드판 안쪽에 있고, 빈칸(0)인지 검증하는 함수
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

        // 1. 가로줄(row) 검사
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

        // 2. 세로줄(column) 검사
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

        // 3. 꽉 찬 줄의 오브젝트 파괴 및 배열 초기화
        foreach (Vector2Int pos in linesToClear)
        {
            if (boardObjects[pos.x, pos.y] != null)
            {
                // 실제 화면에서 블록 조각을 파괴
                Destroy(boardObjects[pos.x, pos.y]);
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
}