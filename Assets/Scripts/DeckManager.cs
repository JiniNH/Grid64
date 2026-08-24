using UnityEngine;

public class DeckManager : MonoBehaviour
{
    // 외부에서 쉽게 접근하기 위한 싱글톤
    public static DeckManager Instance;

    [Header("블록 프리팹 배열(붕어빵 틀)")]
    public GameObject[] blockPrefabs;

    [Header("블록이 생성될 위치 배열")]
    public Transform[] spawnPoints;

    // 현재 덱에 스폰된 실제 블록 오브젝트들을 추적하기 위한 배열
    private GameObject[] currentBlocks = new GameObject[3];
    // 현재 하단 덱에 남아있는 잉여 블록 개수
    private int activeBlocks = 0;

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

    public void SpawnBlocks()
    {
        activeBlocks = 3;

        // 스폰 포인트의 개수(3개)만큼 반복
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            // 가중치를 반영해 블록 선택
            GameObject selectedPrefab = GetWeightedRandomBlock();

            // 선택된 블록을 해당 스폰 위치에 생성
            GameObject newBlock = Instantiate(selectedPrefab, spawnPoints[i].position, Quaternion.identity);

            // 덱에서는 작게 표시
            newBlock.transform.localScale = Vector3.one * 0.4f;

            currentBlocks[i] = newBlock;
        }
        Debug.Log("하단 덱에 가중치 기반 랜덤 블록 3개가 스폰되었습니다!");

        // 새 블록이 스폰되었을 때도 놓을 공간이 있는지 검사
        BoardManager.Instance.CheckGameOver(currentBlocks);
    }

    // 가중치(weight)를 반영해 블록을 랜덤 선택하는 함수
    private GameObject GetWeightedRandomBlock()
    {
        // 1) 전체 가중치 합 구하기
        int totalWeight = 0;
        foreach (GameObject prefab in blockPrefabs)
        {
            BlockInfo info = prefab.GetComponent<BlockInfo>();
            totalWeight += (info != null) ? info.weight : 1;   // 정보 없으면 기본 1
        }

        // 2) 0 ~ 전체가중치 사이 랜덤값 뽑기
        int randomValue = Random.Range(0, totalWeight);

        // 3) 가중치를 누적하며 랜덤값이 걸리는 블록 찾기 (룰렛 방식)
        int cumulative = 0;
        foreach (GameObject prefab in blockPrefabs)
        {
            BlockInfo info = prefab.GetComponent<BlockInfo>();
            cumulative += (info != null) ? info.weight : 1;
            if (randomValue < cumulative)
            {
                return prefab;
            }
        }

        // 4) 혹시 못 찾으면 첫 번째 (안전장치)
        return blockPrefabs[0];
    }

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