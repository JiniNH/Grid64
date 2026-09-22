using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    // 외부에서 쉽게 접근하기 위한 싱글톤
    public static DeckManager Instance;

    [Header("블록 프리팹 배열(붕어빵 틀)")]
    public GameObject[] blockPrefabs;

    [Header("블록이 생성될 위치 배열")]
    public Transform[] spawnPoints;

    [Header("덱 표시 설정")]
    [SerializeField] private float deckScale = 0.4f;          // 덱에서의 축소 비율

    [Header("스마트 스폰")]
    [SerializeField] private float smartSpawnChance = 0.4f;   // 라인 완성 블록이 나올 확률

    [Header("덱 터치 영역")]
    [Tooltip("덱 영역의 위쪽 경계 (이 Y보다 아래를 터치해야 덱으로 인정)")]
    [SerializeField] private float deckTouchTop = -4.2f;
    [Tooltip("덱 영역의 아래쪽 경계")]
    [SerializeField] private float deckTouchBottom = -7.5f;

    // 현재 덱에 스폰된 실제 블록 오브젝트들을 추적하기 위한 배열
    private GameObject[] currentBlocks = new GameObject[3];
    // 현재 하단 덱에 남아있는 잉여 블록 개수
    private int activeBlocks = 0;

    // 스폰 1회당 재사용할 후보 목록 (성능 최적화)
    private List<GameObject> lineCompleters = new List<GameObject>();
    private List<GameObject> placeable = new List<GameObject>();

    // 현재 구역 터치로 잡고 있는 블록
    private BlockDrag grabbedBlock = null;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 보드가 먼저 초기화되도록 살짝 지연 후 스폰
        Invoke(nameof(InitialSpawn), 0.1f);
    }

    void InitialSpawn()
    {
        SpawnBlocks();
    }

    // ===== 구역 터치 처리 =====

    void Update()
    {
        // 게임오버 등으로 시간이 멈춘 상태에서는 입력 무시
        if (Time.timeScale <= 0f) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryGrabFromZone();
        }
        else if (Input.GetMouseButton(0) && grabbedBlock != null)
        {
            grabbedBlock.DragTo(GetMouseWorldPos());
        }
        else if (Input.GetMouseButtonUp(0) && grabbedBlock != null)
        {
            grabbedBlock.ReleaseDrag();
            grabbedBlock = null;
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0f;
        return pos;
    }

    // 터치 위치가 속한 구역의 블록을 집음
    private void TryGrabFromZone()
    {
        Vector3 touch = GetMouseWorldPos();

        // 덱 영역 밖이면 무시 (보드 위 터치 등)
        if (touch.y > deckTouchTop || touch.y < deckTouchBottom) return;

        int zone = GetZoneIndex(touch.x);
        if (zone < 0 || zone >= currentBlocks.Length) return;

        GameObject block = currentBlocks[zone];
        if (block == null) return;

        BlockDrag drag = block.GetComponent<BlockDrag>();
        if (drag == null || !drag.enabled) return;

        drag.BeginDrag(touch);
        grabbedBlock = drag;
    }

    // X좌표가 몇 번 구역에 속하는지 판단 (스폰포인트 사이의 중간값이 경계)
    private int GetZoneIndex(float x)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return -1;

        int nearest = 0;
        float minDist = Mathf.Abs(x - spawnPoints[0].position.x);

        for (int i = 1; i < spawnPoints.Length; i++)
        {
            float dist = Mathf.Abs(x - spawnPoints[i].position.x);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }

    public void SpawnBlocks()
    {
        activeBlocks = 3;
        grabbedBlock = null;

        // 후보 목록을 한 번만 계산해서 3개 스폰에 재사용
        BuildCandidateLists();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject selectedPrefab = GetSmartBlock();

            GameObject newBlock = Instantiate(selectedPrefab, spawnPoints[i].position, Quaternion.identity);
            newBlock.transform.localScale = Vector3.one * deckScale;

            AlignBlockToSpawnPoint(newBlock, spawnPoints[i].position);

            currentBlocks[i] = newBlock;
        }

        Debug.Log($"덱 스폰 완료 (라인완성 후보 {lineCompleters.Count}종 / 배치가능 {placeable.Count}종)");

        // 새 블록이 스폰되었을 때도 놓을 공간이 있는지 검사
        BoardManager.Instance.CheckGameOver(currentBlocks);
    }

    // ===== 스마트 스폰 =====

    // 현재 보드 상태를 기준으로 후보 목록 2종을 미리 계산
    private void BuildCandidateLists()
    {
        lineCompleters.Clear();
        placeable.Clear();

        foreach (GameObject prefab in blockPrefabs)
        {
            if (BoardManager.Instance.CanPlaceAnywhere(prefab))
            {
                placeable.Add(prefab);

                // 놓을 수 있는 블록 중에서만 라인 완성 여부 검사 (불필요한 연산 절약)
                if (BoardManager.Instance.CanCompleteLineWith(prefab))
                {
                    lineCompleters.Add(prefab);
                }
            }
        }
    }

    // 상황에 맞는 블록을 선택 (3단계 폴백)
    private GameObject GetSmartBlock()
    {
        // 1순위: 확률에 당첨되고 라인 완성 가능한 블록이 있으면 그중에서
        if (Random.value < smartSpawnChance && lineCompleters.Count > 0)
        {
            return GetWeightedRandomFrom(lineCompleters);
        }

        // 2순위: 보드에 놓을 수 있는 블록 중에서
        if (placeable.Count > 0)
        {
            return GetWeightedRandomFrom(placeable);
        }

        // 3순위: 놓을 데가 아예 없으면 전체에서 랜덤 (데드락 직전 상황)
        return GetWeightedRandomFrom(new List<GameObject>(blockPrefabs));
    }

    // 주어진 목록 안에서 가중치를 반영해 랜덤 선택
    private GameObject GetWeightedRandomFrom(List<GameObject> candidates)
    {
        if (candidates == null || candidates.Count == 0) return blockPrefabs[0];

        int totalWeight = 0;
        foreach (GameObject prefab in candidates)
        {
            BlockInfo info = prefab.GetComponent<BlockInfo>();
            totalWeight += (info != null) ? info.weight : 1;
        }

        if (totalWeight <= 0) return candidates[0];

        int randomValue = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (GameObject prefab in candidates)
        {
            BlockInfo info = prefab.GetComponent<BlockInfo>();
            cumulative += (info != null) ? info.weight : 1;
            if (randomValue < cumulative) return prefab;
        }

        return candidates[0];   // 안전장치
    }

    // ===== 배치 보조 =====

    // 블록의 시각적 중심을 스폰 포인트에 맞춰 정렬
    private void AlignBlockToSpawnPoint(GameObject block, Vector3 spawnPos)
    {
        SpriteRenderer[] renderers = block.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return;

        // 모든 파트를 감싸는 영역(bounds) 계산
        Bounds bounds = renderers[0].bounds;
        foreach (SpriteRenderer sr in renderers)
        {
            bounds.Encapsulate(sr.bounds);
        }

        // 실제 중심과 스폰 위치의 차이만큼 보정
        Vector3 alignOffset = spawnPos - bounds.center;
        alignOffset.z = 0f;                       // Z축은 건드리지 않음
        block.transform.position += alignOffset;
    }

    // ===== 덱 소모 =====

    // 블록이 보드판에 성공적으로 배치될 때마다 호출될 함수
    public void BlockPlaced()
    {
        activeBlocks--; // 남은 블록 수 1개 감소

        // 덱이 텅 비었다면 3개 새로 스폰
        if (activeBlocks <= 0)
        {
            Debug.Log("덱을 모두 소모하여 새로 리필합니다!");
            SpawnBlocks();  // 이 안에서 CheckGameOver()가 자동으로 호출됨
        }
        else
        {
            // 블록을 하나 배치한 직후, 남은 블록들이 들어갈 자리가 있는지 검사
            BoardManager.Instance.CheckGameOver(currentBlocks);
        }
    }
}