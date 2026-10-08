using UnityEngine;
using System.Collections.Generic;

// 메인 메뉴 배경: 블록 조각이 천천히 떠오르며 회전하는 연출
public class MenuBackground : MonoBehaviour
{
    [Header("블록 칸 스프라이트 (색깔별로 여러 개)")]
    public Sprite[] cellSprites;

    [Header("크기")]
    [Tooltip("칸 한 개의 크기 (화면 세로 대비 비율, 0.08 = 8%)")]
    [Range(0.02f, 0.2f)] public float cellScreenRatio = 0.08f;
    [Tooltip("조각마다 무작위로 곱해지는 크기 범위")]
    public float sizeMin = 0.8f;
    public float sizeMax = 1.3f;

    [Header("개수와 간격")]
    public int pieceCount = 12;
    [Tooltip("1 = 칸이 딱 붙음, 1보다 크면 벌어짐")]
    public float cellSpacing = 1f;

    [Header("움직임")]
    public float minSpeed = 0.25f;         // 초당 상승 거리 (월드 단위)
    public float maxSpeed = 0.6f;
    public float maxRotateSpeed = 15f;     // 초당 회전 각도
    public float swayAmount = 0.25f;       // 좌우 흔들림 폭

    [Header("표현")]
    [Range(0f, 1f)] public float alpha = 0.22f;
    public int sortingOrder = -10;

    // 게임에 나오는 블록 모양들 (칸 좌표)
    private static readonly Vector2Int[][] Shapes =
    {
        new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },   // 2x2
        new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) },   // T
        new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,0) },   // L
        new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) },   // S
        new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },                        // 1x3
        new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) },   // 1x4
        new[] { new Vector2Int(0,0) },                                                                   // 1x1
    };

    private class Piece
    {
        public Transform t;
        public float speed, rotSpeed, swayPhase, baseX;
        public float radius;      // 크기 1 기준 반경
        public float halfSize;    // 현재 크기 기준 반경
    }

    private readonly List<Piece> pieces = new List<Piece>();
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (cam == null || cellSprites == null || cellSprites.Length == 0) return;

        for (int i = 0; i < pieceCount; i++)
        {
            Piece p = CreatePiece();
            // 처음에는 화면 전체에 흩뿌려서 시작 (빈 화면에서 올라오기 시작하면 어색함)
            Respawn(p, Random.Range(ViewBottom(), ViewTop()));
            pieces.Add(p);
        }
    }

    void Update()
    {
        if (cam == null) return;

        float top = ViewTop();

        foreach (Piece p in pieces)
        {
            Vector3 pos = p.t.position;
            pos.y += p.speed * Time.deltaTime;
            pos.x = p.baseX + Mathf.Sin(Time.time * 0.6f + p.swayPhase) * swayAmount;
            p.t.position = pos;
            p.t.Rotate(0f, 0f, p.rotSpeed * Time.deltaTime);

            // 화면 위로 완전히 벗어나면 아래에서 다시 등장
            if (pos.y - p.halfSize > top)
            {
                Respawn(p, ViewBottom() - p.halfSize);
            }
        }
    }

    private Piece CreatePiece()
    {
        Vector2Int[] shape = Shapes[Random.Range(0, Shapes.Length)];
        Sprite sprite = cellSprites[Random.Range(0, cellSprites.Length)];

        // 칸 한 개의 목표 크기 (화면 세로 대비)
        float cellWorld = cam.orthographicSize * 2f * cellScreenRatio;

        // 스프라이트 원래 크기와 상관없이 목표 크기가 되도록 배율 계산
        float spriteSize = Mathf.Max(0.0001f, sprite.bounds.size.x);
        float cellScale = cellWorld / spriteSize;

        // 칸 사이 간격 (1이면 딱 붙음)
        float step = cellWorld * cellSpacing;

        // 모양의 중심을 계산해서 회전축을 가운데로
        Vector2 center = Vector2.zero;
        foreach (Vector2Int c in shape) center += (Vector2)c;
        center /= shape.Length;

        GameObject root = new GameObject("BgPiece");
        root.transform.SetParent(transform, false);

        float radius = 0f;
        foreach (Vector2Int c in shape)
        {
            GameObject cell = new GameObject("Cell");
            cell.transform.SetParent(root.transform, false);

            Vector2 offset = ((Vector2)c - center) * step;
            cell.transform.localPosition = offset;
            cell.transform.localScale = Vector3.one * cellScale;
            radius = Mathf.Max(radius, offset.magnitude + cellWorld * 0.75f);

            SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 1f, 1f, alpha);
            sr.sortingOrder = sortingOrder;
        }

        return new Piece { t = root.transform, radius = radius };
    }

    private void Respawn(Piece p, float y)
    {
        float halfW = cam.orthographicSize * cam.aspect;
        float size = Random.Range(sizeMin, sizeMax);

        p.baseX = cam.transform.position.x + Random.Range(-halfW, halfW);
        p.speed = Random.Range(minSpeed, maxSpeed);
        p.rotSpeed = Random.Range(-maxRotateSpeed, maxRotateSpeed);
        p.swayPhase = Random.Range(0f, Mathf.PI * 2f);
        p.halfSize = p.radius * size;

        p.t.localScale = Vector3.one * size;
        p.t.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        p.t.position = new Vector3(p.baseX, y, 0f);
    }

    private float ViewTop()    => cam.transform.position.y + cam.orthographicSize;
    private float ViewBottom() => cam.transform.position.y - cam.orthographicSize;
}