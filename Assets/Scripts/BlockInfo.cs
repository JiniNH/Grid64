using UnityEngine;

public class BlockInfo : MonoBehaviour
{
    public enum Tier { T1, T2, T3, T4 }

    [Header("블록 밸런스 정보")]
    public Tier tier = Tier.T2;     // 이 블록의 티어
    public int weight = 10;         // 출현 가중치 (높을수록 자주 나옴)

    [Header("이펙트 설정")]
    [Tooltip("체크하면 아래 색을 파편 색으로 사용. 끄면 스프라이트에서 자동 추출")]
    public bool useCustomFragmentColor = false;
    public Color fragmentColor = Color.white;   // 라인 클리어 시 튀는 파편 색
}