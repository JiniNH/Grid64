using UnityEngine;

// 장애물 모드의 돌덩이 — 줄이 지워질 때마다 내구도 -1, 0이 되면 부서짐
public class Stone : MonoBehaviour
{
    [Header("내구도별 색 (1 / 2 / 3)")]
    public Color[] hpColors =
    {
        new Color(0.80f, 0.82f, 0.86f),   // 내구도 1: 연한 회색
        new Color(0.60f, 0.64f, 0.70f),   // 내구도 2: 중간 회색
        new Color(0.40f, 0.44f, 0.50f)    // 내구도 3: 진한 회색
    };

    [Header("표시")]
    public SpriteRenderer body;                       // 돌 본체 (흰색 스프라이트에 색을 입힘)
    public GameObject[] cracks = new GameObject[0];   // 금 단계 (0: 첫 번째 금, 1: 두 번째 금)

    public int MaxHp { get; private set; }
    public int Hp { get; private set; }
    public Color BodyColor => body != null ? body.color : Color.gray;

    public void Init(int hp)
    {
        MaxHp = Mathf.Clamp(hp, 1, hpColors.Length);
        Hp = MaxHp;
        if (body != null) body.color = hpColors[MaxHp - 1];
        RefreshCracks();
    }

    // 한 대 맞음. 부서졌으면 true
    public bool Hit()
    {
        Hp--;
        Debug.Log($"🪨 돌 내구도 {Hp + 1} → {Hp}");
        RefreshCracks();
        return Hp <= 0;
    }

    private void RefreshCracks()
    {
        // 맞은 적 없으면 금 없음.
        // 맞았으면 "남은 내구도" 기준: 남은 1 = 금 전부(대각선, 깨지기 직전), 남은 2 = 금 1개
        int show = (Hp >= MaxHp) ? 0 : Mathf.Clamp(cracks.Length - (Hp - 1), 0, cracks.Length);

        for (int i = 0; i < cracks.Length; i++)
            if (cracks[i] != null) cracks[i].SetActive(i < show);
    }
}

// 장애물 모드 난이도 구간 (BoardManager 인스펙터에서 편집)
[System.Serializable]

public class StoneTier
{
    [Tooltip("이 턴부터 적용 (표는 오름차순으로)")]
    public int fromTurn = 1;
    [Tooltip("N턴마다 돌 1개")]
    public int spawnEvery = 1;
    [Tooltip("보드 위 동시 최대")]
    public int maxStones = 4;
    [Tooltip("한 번에 생성할 개수")]
    public int count = 1;
    [Header("내구도 비율 (합이 100일 필요 없음)")]
    public int hp1 = 100;
    public int hp2 = 0;
    public int hp3 = 0;
}