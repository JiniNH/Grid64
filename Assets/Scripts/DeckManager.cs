using UnityEngine;

public class DeckManager : MonoBehaviour
{
    // 외부에서 쉽게 접근하기 위한 싱글톤
    public static DeckManager Instance;

    [Header("블록 프리팹 배열(붕어빵 틀)")]
    public GameObject[] blockPrefabs;

    [Header("블록이 생성될 위치 배열")]
    public Transform[] spawnPoints;

    // 현재 하단 덱에 남아있는 잉여 블록 개수
    private int activeBlocks = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 게임 시작 시 3개의 블록을 스폰하는 함수 호출
        SpawnBlocks();    
    }

    public void SpawnBlocks()
    {
        activeBlocks = 3;
        
        // 스폰 포인트의 개수(3개)만큼 반복
        for (int i = 0 ; i < spawnPoints.Length; i++)
        {
            // 0부터 블록 종류 개수(3) 미만의 랜덤한 숫자(인덱스) 뽑기
            int randomIndex = Random.Range(0, blockPrefabs.Length);

            // 선택된 랜덤 블록을 해당 스폰 위치에 생성
            Instantiate(blockPrefabs[randomIndex], spawnPoints[i].position, Quaternion.identity);
        }
        Debug.Log("하단 덱에 랜덤 블록 3개가 스폰되었습니다!");
    }

    // 블록이 보드판에 성공적으로 배치될 때마다 호출될 함수
    public void BlockPlaced()
    {
        activeBlocks --; // 남은 블록 수 1개 감소

        // 덱이 텅 비었다면 3개 새로 스폰
        if (activeBlocks <= 0)
        {
            Debug.Log("덱을 모두 소모하여 새로 리필합니다!");
            SpawnBlocks();
        }
    }
}
