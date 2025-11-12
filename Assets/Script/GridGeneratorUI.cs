using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class GridGeneratorUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RectTransform board;          // UI panel that has a GridLayoutGroup
    public CardUI cardPrefab;            // your Card prefab (with CardUI)
    public Vector2 spacing = new Vector2(8, 8);
    public List<Sprite> faceSprites;     // supply >= ceil(rows*cols/2)

    GridLayoutGroup grid;
    Vector2 lastBoardSize = Vector2.zero;

    void Awake()
    {
        if (!board) board = (RectTransform)transform;
        grid = board.GetComponent<GridLayoutGroup>();
        if (!grid) grid = board.gameObject.AddComponent<GridLayoutGroup>();

        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        grid.spacing = spacing;
        grid.padding = new RectOffset(0, 0, 0, 0);
    }

    // Build the board, return all cards created
    public List<CardUI> Generate(int rows, int cols, Random rng, GameControllerUI owner, AudioManager audioMgr)
    {
        // clear old
        for (int i = board.childCount - 1; i >= 0; i--)
            Destroy(board.GetChild(i).gameObject);

        grid.spacing = spacing;
        FitCellSize(rows, cols);

        var pairs = BuildShuffledPairs(rows * cols, rng);
        var all = new List<CardUI>(pairs.Count);

        for (int i = 0; i < pairs.Count; i++)
        {
            var entry = pairs[i];
            var card = Instantiate(cardPrefab, board);
            var rt = (RectTransform)card.transform;
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition3D = Vector3.zero;

            card.Init(entry.id, entry.sprite, owner, audioMgr);
            all.Add(card);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(board);
        CacheBoardSize();
        return all;
    }

    // -------- helpers --------
    void FitCellSize(int rows, int cols)
    {
        var r = board.rect;

        float totalH = spacing.x * (cols - 1);
        float totalV = spacing.y * (rows - 1);

        float cellW = Mathf.Floor((r.width - totalH) / cols);
        float cellH = Mathf.Floor((r.height - totalV) / rows);
        float size = Mathf.Floor(Mathf.Min(cellW, cellH));

        grid.cellSize = new Vector2(size, size);
    }

    struct FaceEntry { public int id; public Sprite sprite; }

    List<FaceEntry> BuildShuffledPairs(int totalCards, Random rng)
    {
        int pairs = Mathf.CeilToInt(totalCards / 2f);
        var list = new List<FaceEntry>(pairs * 2);

        for (int id = 0; id < pairs; id++)
        {
            var s = faceSprites.Count > 0 ? faceSprites[id % faceSprites.Count] : null;
            list.Add(new FaceEntry { id = id, sprite = s });
            list.Add(new FaceEntry { id = id, sprite = s });
        }
        while (list.Count > totalCards) list.RemoveAt(list.Count - 1);

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

#if UNITY_EDITOR
    // live resize while running if you tweak board size
    void Update()
    {
        var r = board.rect;
        if (!Mathf.Approximately(lastBoardSize.x, r.width) ||
            !Mathf.Approximately(lastBoardSize.y, r.height))
        {
            lastBoardSize = new Vector2(r.width, r.height);
            int count = board.childCount;
            int c = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count)));
            int rws = Mathf.Max(1, Mathf.CeilToInt(count / (float)c));
            FitCellSize(rws, c);
            LayoutRebuilder.ForceRebuildLayoutImmediate(board);
        }
    }
#endif

    void CacheBoardSize()
    {
        var r = board.rect;
        lastBoardSize = new Vector2(r.width, r.height);
    }
}
