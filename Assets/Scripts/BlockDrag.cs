using UnityEngine;

public class BlockDrag : MonoBehaviour
{
    private Vector3 offset;
    private Vector3 startPosition;

    // 1. 마우스를 클릭하는 순간 1회 실행
    void OnMouseDown()
    {
        // 나중에 스냅 실패 시 돌아올 원래 우치 저장
        startPosition = transform.position;

        // 마우스의 화면 좌표를 게임 좌표로 변환
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 마우스 클릭 지점과 블록 중심점 사이의 거리(offset)계산
        offset = transform.position - mousePos;
    }

    // 2. 마우스를 누른 채로 움직이는 동안 계속 실행
    void OnMouseDrag()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 마우스 위치 + 오프셋 위치로 블록 이동 (Z축은 0으로 고정)
        transform.position = new Vector3(mousePos.x + offset.x, mousePos.y + offset.y, 0);
    }

    // 3. 마우스 클릭을 떼는 순간 1회 실행
    void OnMouseUp()
    {
        bool canPlace = true;

        // 1. 블록에 달려있는 모든 하위 파트들을 순회하며 검사
        foreach (Transform child in transform)
        {
            Vector2Int gridIndex = BoardManager.Instance.GetGridIndex(child.position);

            // [에러 원인 A] 보드판 밖으로 나갔는지 체크
            if (gridIndex.x < 0 || gridIndex.x >= BoardManager.Instance.width || gridIndex.y < 0 || gridIndex.y >= BoardManager.Instance.height)
            {
                Debug.Log($"[배치 실패] 범위를 벗어났습니다! 시도한 인덱스: ({gridIndex.x}, {gridIndex.y})");
                canPlace = false;
                break;
            }
            // [에러 원인 B] 이미 블록이 놓여 있는지(배열 값이 1인지) 체크
            else if (BoardManager.Instance.gridData[gridIndex.x, gridIndex.y] == 1)
            {
                Debug.Log($"[배치 실패] 이미 블록이 있습니다! 시도한 인덱스: ({gridIndex.x}, {gridIndex.y})");
                canPlace = false;
                break;
            }
        }

        // 2. 판정 결과에 따른 처리
        if (canPlace)
        {
            Vector2Int parentGridIndex = BoardManager.Instance.GetGridIndex(transform.position);
            transform.position = BoardManager.Instance.GetWorldPosition(parentGridIndex.x, parentGridIndex.y);

            foreach (Transform child in transform)
            {
                Vector2Int gridIndex = BoardManager.Instance.GetGridIndex(child.position);
                
                // 숫자 데이터 배열을 1로 업데이트
                BoardManager.Instance.gridData[gridIndex.x, gridIndex.y] = 1; 
                
                // 시각적 오브젝트 데이터를 배열에 저장
                BoardManager.Instance.boardObjects[gridIndex.x, gridIndex.y] = child.gameObject; 
            }

            this.enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;

            // 빙고가 완성되었는지 검사 실행
            BoardManager.Instance.CheckAndClearLines();

            // 덱 매니저에게 블록 하나 썻다고 알림
            DeckManager.Instance.BlockPlaced();
        }
        else
        {
            transform.position = startPosition; // 실패 시 덱으로 원복
        }
    }
}
