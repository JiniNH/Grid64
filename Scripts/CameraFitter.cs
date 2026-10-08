using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFitter : MonoBehaviour
{
    [Header("가로 기준")]
    [Tooltip("화면 가로에 반드시 들어와야 할 월드 폭 (보드 8칸 + 좌우 여백)")]
    public float targetWidth = 8.6f;

    [Header("기준 비율 (이보다 넓은 화면은 세로 시야를 이 비율에 고정)")]
    public float referenceWidth = 1080f;
    public float referenceHeight = 1920f;

    [Header("세로 배치")]
    [Tooltip("보드 윗변과 화면 상단 사이 최소 공간 (점수·게이지 UI)")]
    public float topSpace = 3.2f;
    public float boardHalfHeight = 4f;
    [Tooltip("보드 아랫변과 화면 하단 사이 최소 공간 (덱 + 배너)")]
    public float bottomSpace = 4.2f;
    [Tooltip("전체 레이아웃 미세 조정 (양수 = 내용물이 아래로)")]
    public float verticalOffset = 0f;

    private Camera cam;
    private int lastWidth, lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    void Update()
    {
        // 해상도 변경(폴더블 접기/펼치기, 회전, 에디터 창 조절) 대응
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            Fit();
        }
    }

    public void Fit()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (cam == null || !cam.orthographic) return;

        float screenAspect = (float)Screen.width / Screen.height;
        if (screenAspect <= 0f) return;

        float refAspect = referenceWidth / referenceHeight;

        // 세로로 긴 화면: 가로 폭에 맞춤 (기존 동작)
        float widthFitSize = (targetWidth * 0.5f) / screenAspect;
        // 넓적한 화면: 9:16 기준 세로 시야로 고정
        float refSize = (targetWidth * 0.5f) / refAspect;

        // 둘 중 큰 쪽 → 어떤 비율에서도 보드와 덱이 모두 들어옴
        cam.orthographicSize = Mathf.Max(widthFitSize, refSize);

        float contentCenterY = (topSpace - bottomSpace) * -0.5f;
        float camY = contentCenterY + verticalOffset;
        transform.position = new Vector3(0f, camY, transform.position.z);
    }
}