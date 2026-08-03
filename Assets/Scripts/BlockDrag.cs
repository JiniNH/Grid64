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

    void OnMouseDown()
    {
        // 집어들면 원래 크기(1) 적용
        transform.localScale = Vector3.one;

        // 드래그 시작 시 모든 Part를 맨 앞으로
        foreach (var pair in originalOrders)
        {
            pair.Key.sortingOrder = dragSortingOrder;
        }

        startPosition = transform.position;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - mousePos;

        for (int i = 0; i < shadows.Count; i++)
        {
            shadowRenderers[i].enabled = true;
            shadowRenderers[i].sortingOrder = dragSortingOrder - 1;     // 그림자도 앞으로 (part-1)
            shadows[i].localPosition = shadowBasePositions[i] + shadowOffset;
        }

        RetroAudio.Instance.PlayPickup();
    }

    void OnMouseDrag()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x + offset.x, mousePos.y + offset.y, 0);
    }

    void OnMouseUp()
    {
        // 드래그 끝나면 원래 order로 복구
        foreach(var pair in originalOrders)
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
                Debug.Log($"[배치 실패] 범위를 벗어났습니다! 시도한 인덱스: ({gridIndex.x}, {gridIndex.y})");
                canPlace = false;
                break;
            }
            else if (BoardManager.Instance.gridData[gridIndex.x, gridIndex.y] == 1)
            {
                Debug.Log($"[배치 실패] 이미 블록이 있습니다! 시도한 인덱스: ({gridIndex.x}, {gridIndex.y})");
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

            this.enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;

            foreach (var sr in shadowRenderers) sr.enabled = false;

            BoardManager.Instance.CheckAndClearLines();
            DeckManager.Instance.BlockPlaced();
        }
        else
        {
            // [스케일] 배치 실패로 덱에 돌아가면 다시 작게
            transform.position = startPosition;
            transform.localScale = Vector3.one * deckScale;
        }
    }
}