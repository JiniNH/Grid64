using UnityEngine;
using System.Collections.Generic;

public class BlockDrag : MonoBehaviour
{
    private Vector3 offset;
    private Vector3 startPosition;

    [Header("덱 표시 크기")]
    [SerializeField] private float deckScale = 0.4f;   // 덱에 있을 때 축소 비율

    [Header("Shadow 연출")]
    [SerializeField] private Vector3 shadowOffset = new Vector3(0.2f, -0.2f, 0f);
    [SerializeField] private float shadowScaleUp = 1.15f;
    [SerializeField] private float shadowAlpha = 0.82f;

    private List<Transform> shadows = new List<Transform>();
    private List<Vector3> shadowBasePositions = new List<Vector3>();
    private List<SpriteRenderer> shadowRenderers = new List<SpriteRenderer>();

    [Header("드래그 시 최상단 표시")]
    [SerializeField] private int dragSortingOrder = 100;    // 드래그 중일 때 order

    private Dictionary<SpriteRenderer, int> originalOrders = new Dictionary<SpriteRenderer, int>();

    [Header("드래그 감도")]
    [SerializeField] private float dragSmoothSpeed = 80f;    // 값이 클수록 빠르게 따라옴

    [Header("집었을 때 손가락 위 오프셋")]
    [Tooltip("손가락에 블록이 가리지 않도록 위로 띄우는 거리")]
    [SerializeField] private float grabYOffset = 1.2f;

    private Vector3 targetPosition;
    private bool isDragging = false;

    void Start()
    {
        CreateShadows();

        // 각 Part의 원래 sorting order 저장 (그림자 제외)
        foreach (Transform child in transform)
        {
            if (child.name == "Shadow_Auto") continue;
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null) originalOrders[sr] = sr.sortingOrder;
        }

        // 콜라이더는 더 이상 덱 터치에 쓰이지 않으므로 꺼둠
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;
    }

    void CreateShadows()
    {
        List<Transform> parts = new List<Transform>();
        foreach (Transform child in transform) parts.Add(child);

        foreach (Transform part in parts)
        {
            SpriteRenderer partSR = part.GetComponent<SpriteRenderer>();
            if (partSR == null) continue;

            GameObject shadowObj = Instantiate(part.gameObject, transform);
            shadowObj.name = "Shadow_Auto";

            foreach (var c in shadowObj.GetComponents<MonoBehaviour>()) Destroy(c);
            foreach (var col in shadowObj.GetComponents<Collider2D>()) Destroy(col);
            foreach (Transform grand in shadowObj.transform) Destroy(grand.gameObject);

            SpriteRenderer shadowSR = shadowObj.GetComponent<SpriteRenderer>();
            shadowSR.color = new Color(0f, 0f, 0f, shadowAlpha);
            shadowSR.sortingOrder = partSR.sortingOrder - 1;

            Transform st = shadowObj.transform;
            st.localScale = part.localScale * shadowScaleUp;

            shadows.Add(st);
            shadowBasePositions.Add(part.localPosition);
            shadowRenderers.Add(shadowSR);

            st.localPosition = part.localPosition;
            shadowSR.enabled = false;
        }
    }

    void Update()
    {
        if (!isDragging) return;

        // 프레임레이트와 무관하게 일정한 추종 속도 (오버슈트 방지)
        float t = 1f - Mathf.Exp(-dragSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
    }

    // ===== DeckManager가 호출하는 공개 메서드 =====

    // 구역 터치로 집혔을 때
    public void BeginDrag(Vector3 touchWorldPos)
    {
        if (isDragging) return;

        startPosition = transform.position;

        // 집어들면 원래 크기(1) 적용
        transform.localScale = Vector3.one;

        // 드래그 시작 시 모든 Part를 맨 앞으로
        foreach (var pair in originalOrders)
        {
            pair.Key.sortingOrder = dragSortingOrder;
        }

        for (int i = 0; i < shadows.Count; i++)
        {
            shadowRenderers[i].enabled = true;
            shadowRenderers[i].sortingOrder = dragSortingOrder - 1;
            shadows[i].localPosition = shadowBasePositions[i] + shadowOffset;
        }

        // 손가락보다 위에 블록이 오도록 오프셋 고정
        offset = new Vector3(0f, grabYOffset, 0f);

        targetPosition = new Vector3(touchWorldPos.x + offset.x, touchWorldPos.y + offset.y, 0f);
        isDragging = true;

        RetroAudio.Instance.PlayPickup();
    }

    // 드래그 중 목표 위치 갱신
    public void DragTo(Vector3 touchWorldPos)
    {
        if (!isDragging) return;
        targetPosition = new Vector3(touchWorldPos.x + offset.x, touchWorldPos.y + offset.y, 0f);
    }

    // 손을 뗐을 때 배치 시도
    public void ReleaseDrag()
    {
        if (!isDragging) return;
        isDragging = false;

        // 스냅 판정은 보간 중간값이 아닌 최종 목표 위치 기준
        transform.position = targetPosition;

        // 드래그 끝나면 원래 order로 복구
        foreach (var pair in originalOrders)
        {
            pair.Key.sortingOrder = pair.Value;
        }

        for (int i = 0; i < shadows.Count; i++)
        {
            shadows[i].localPosition = shadowBasePositions[i];
            shadowRenderers[i].enabled = false;
        }

        bool canPlace = true;

        foreach (Transform child in transform)
        {
            if (child.name == "Shadow_Auto") continue;

            Vector2Int gridIndex = BoardManager.Instance.GetGridIndex(child.position);

            if (gridIndex.x < 0 || gridIndex.x >= BoardManager.Instance.width || gridIndex.y < 0 || gridIndex.y >= BoardManager.Instance.height)
            {
                canPlace = false;
                break;
            }
            else if (BoardManager.Instance.gridData[gridIndex.x, gridIndex.y] == 1)
            {
                canPlace = false;
                break;
            }
        }

        if (canPlace)
        {
            RetroAudio.Instance.PlayDrop();

            Vector2Int parentGridIndex = BoardManager.Instance.GetGridIndex(transform.position);
            transform.position = BoardManager.Instance.GetWorldPosition(parentGridIndex.x, parentGridIndex.y);

            foreach (Transform child in transform)
            {
                if (child.name == "Shadow_Auto") continue;

                Vector2Int gridIndex = BoardManager.Instance.GetGridIndex(child.position);
                BoardManager.Instance.gridData[gridIndex.x, gridIndex.y] = 1;
                BoardManager.Instance.boardObjects[gridIndex.x, gridIndex.y] = child.gameObject;
            }

            // 블록 배치 점수 (칸 당 1점)
            int blockCellCount = 0;
            foreach (Transform child in transform)
            {
                if (child.name == "Shadow_Auto") continue;
                blockCellCount++;
            }
            BoardManager.Instance.AddScore(blockCellCount);

            this.enabled = false;

            foreach (var sr in shadowRenderers) sr.enabled = false;

            BoardManager.Instance.CheckAndClearLines();
            DeckManager.Instance.BlockPlaced();
        }
        else
        {
            // 배치 실패 → 덱으로 복귀
            transform.position = startPosition;
            transform.localScale = Vector3.one * deckScale;
        }
    }
}