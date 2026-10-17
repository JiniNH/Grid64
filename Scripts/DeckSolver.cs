using UnityEngine;
using System.Collections.Generic;

// 8x8 보드를 64비트 정수(비트보드) 하나로 표현해서
// "덱 블록들을 어떤 순서로든 전부 놓을 수 있는지"를 빠르게 검사한다.
//  - 칸 하나 = 비트 하나 (x, y) → y * 8 + x 번째 비트
//  - 블록이 들어가는지 = (보드 & 블록) == 0
//  - 줄 클리어까지 시뮬레이션해서, 놓는 도중 줄이 지워지며 생기는 자리도 반영
public static class DeckSolver
{
    private const int W = 8;
    private const int H = 8;

    private static readonly ulong[] RowMasks = new ulong[H];
    private static readonly ulong[] ColMasks = new ulong[W];

    // 프리팹별로 "보드 안에 놓을 수 있는 모든 위치"의 마스크를 한 번만 계산해 둠
    private static readonly Dictionary<GameObject, ulong[]> placementCache = new Dictionary<GameObject, ulong[]>();

    static DeckSolver()
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                RowMasks[y] |= Bit(x, y);
                ColMasks[x] |= Bit(x, y);
            }
    }

    public static ulong Bit(int x, int y) => 1UL << (y * W + x);

    // 블록 프리팹이 보드 안에 들어가는 모든 위치의 마스크
    public static ulong[] GetPlacements(GameObject prefab)
    {
        if (placementCache.TryGetValue(prefab, out ulong[] cached)) return cached;

        // Part들의 칸 오프셋 (BoardManager.CanPlaceBlockAt과 같은 기준)
        List<Vector2Int> offsets = new List<Vector2Int>();
        foreach (Transform child in prefab.transform)
        {
            if (child.name == "Shadow_Auto") continue;
            Vector3 p = child.localPosition;
            offsets.Add(new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)));
        }

        List<ulong> masks = new List<ulong>();
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                ulong m = 0;
                bool inside = true;

                foreach (Vector2Int o in offsets)
                {
                    int cx = x + o.x;
                    int cy = y + o.y;
                    if (cx < 0 || cx >= W || cy < 0 || cy >= H) { inside = false; break; }
                    m |= Bit(cx, cy);
                }

                if (inside) masks.Add(m);
            }
        }

        ulong[] result = masks.ToArray();
        placementCache[prefab] = result;
        return result;
    }

    // 블록들을 어떤 순서로든 전부 놓을 수 있으면 true
    // board     : 채워진 칸 전체 (일반 블록 + 돌)
    // fixedMask : 줄이 지워져도 남는 칸 (내구도 2 이상 돌) — 안전하게 "안 지워짐"으로 계산
    public static bool CanPlaceAll(ulong board, ulong fixedMask, List<ulong[]> pieces)
    {
        if (pieces == null || pieces.Count == 0) return true;
        return Solve(board, fixedMask, pieces, 0);
    }

    private static bool Solve(ulong board, ulong fixedMask, List<ulong[]> pieces, int usedFlags)
    {
        int n = pieces.Count;
        int allUsed = (1 << n) - 1;

        for (int i = 0; i < n; i++)
        {
            if ((usedFlags & (1 << i)) != 0) continue;

            // 아직 안 쓴 블록 중 같은 모양이 앞에 있으면 그쪽에서 이미 시도했으므로 건너뜀
            bool duplicate = false;
            for (int j = 0; j < i; j++)
            {
                if ((usedFlags & (1 << j)) == 0 && pieces[j] == pieces[i]) { duplicate = true; break; }
            }
            if (duplicate) continue;

            int nextFlags = usedFlags | (1 << i);
            bool isLast = nextFlags == allUsed;

            foreach (ulong m in pieces[i])
            {
                if ((board & m) != 0) continue;   // 겹침
                if (isLast) return true;           // 마지막 블록은 들어가기만 하면 성공

                if (Solve(PlaceAndClear(board, m, fixedMask), fixedMask, pieces, nextFlags))
                    return true;
            }
        }
        return false;
    }

    // 블록을 놓고, 꽉 찬 가로·세로 줄을 지운 보드
    private static ulong PlaceAndClear(ulong board, ulong piece, ulong fixedMask)
    {
        board |= piece;

        ulong clear = 0;
        for (int i = 0; i < H; i++)
            if ((board & RowMasks[i]) == RowMasks[i]) clear |= RowMasks[i];
        for (int i = 0; i < W; i++)
            if ((board & ColMasks[i]) == ColMasks[i]) clear |= ColMasks[i];

        if (clear == 0) return board;

        // 지워지는 줄의 칸은 비우되, 단단한 돌은 남김
        return (board & ~clear) | (fixedMask & clear);
    }
}