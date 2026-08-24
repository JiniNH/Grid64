using UnityEngine;

public class BlockInfo : MonoBehaviour
{
    public enum Tier { T1, T2, T3, T4 }

    [Header("블록 밸런스 정보")]
    public Tier tier = Tier.T2;     // 이 블록의 티어
    public int weight = 10;         // 출현 가중치 (높을수록 자주 나옴)
}
