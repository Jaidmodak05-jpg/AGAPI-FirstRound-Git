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
    public List<Sprite> faceSprites;     // supply at least ceil(rows*cols/2)

    private GridLayoutGroup grid;
    private Vector2 lastBoardSize = Vector2.zero;

    void Awake()
    {
        if (!board) board = (RectTransform)transform;

        grid = board.GetComponent<GridLayoutGroup>();
        if (!grid) grid = board.gameObject.AddComponent<GridLayoutGroup>();

        // sensible defaults – cell size is computed per Generate()
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        grid.spacing = spacing;
        grid.padding = new RectOffset(0, 0, 0, 0);
    }

    // ----------------------------
    // MAIN ENTRY POINT
    // ----------------------------
    public List<CardUI> Generate(int rows, int cols, Random rng, GameControllerUI owner, AudioManager audioMgr)
    {
        // Clear old children
        for (int i = board.childCount - 1; i >= 0; i--)
            Destroy(board.GetChild(i).gameObject);

        // Size cells so the grid fits inside the board
        grid.spacing = spacing;
        FitCellSize(rows, cols);

        // Build (id, sprite) pairs and shuffle them together
        var faces = BuildShuffledPairs(rows * cols, rng);

        var all = new List<CardUI>(rows * cols);
        for (int i = 0; i < faces.Count; i++)
        {
            var entry = faces[i];

            var card = Instantiate(cardPrefab, board);
            var rt = (RectTransform)card.transform;
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition3D = Vector3.zero;

            // IMPORTANT: id and sprite come from the same shuffled entry
            card.Init(entry.id, entry.sprite, owner, audioMgr);

            all.Add(card);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(board);
        CacheBoardSize();
        return all;
    }

    // Compute cell size so rows x cols (plus spacing) fits board rect
    private void FitCellSize(int rows, int cols)
    {
        var r = board.rect;

        float totalHSpacing = spacing.x * (cols - 1);
        float totalVSpacing = spacing.y * (rows - 1);

        float cellW = Mathf.Floor((r.width - totalHSpacing) / cols);
        float cellH = Mathf.Floor((r.height - totalVSpacing) / rows);
        float size = Mathf.Floor(Mathf.Min(cellW, cellH));

        grid.cellSize = new Vector2(size, size);
    }

    // Pair data kept together through shuffle
    private struct FaceEntry { public int id; public Sprite sprite; }

    // Build 2 entries per id, then shuffle with System.Random (deterministic per run)
    private List<FaceEntry> BuildShuffledPairs(int totalCards, Random rng)
    {
        int neededPairs = Mathf.CeilToInt(totalCards / 2f);
        var list = new List<FaceEntry>(neededPairs * 2);

        // Cycle sprites if the list is shorter than needed
        for (int id = 0; id < neededPairs; id++)
        {
            var s = faceSprites.Count > 0
                ? faceSprites[id % faceSprites.Count]
                : null;

            list.Add(new FaceEntry { id = id, sprite = s });
            list.Add(new FaceEntry { id = id, sprite = s });
        }

        // Trim in case of odd total
        while (list.Count > totalCards)
            list.RemoveAt(list.Count - 1);

        // Fisher–Yates using System.Random so caller controls seed if desired
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private bool BoardSizeChanged()
    {
        var r = board.rect;
        return !Mathf.Approximately(lastBoardSize.x, r.width) ||
               !Mathf.Approximately(lastBoardSize.y, r.height);
    }

    private void CacheBoardSize()
    {
        var r = board.rect;
        lastBoardSize = new Vector2(r.width, r.height);
    }

#if UNITY_EDITOR
    // Live-resize during Play if you tweak the Board rect
    void Update()
    {
        if (grid && BoardSizeChanged())
        {
            CacheBoardSize();
            int count = board.childCount;
            int cols = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count)));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));
            FitCellSize(rows, cols);
            LayoutRebuilder.ForceRebuildLayoutImmediate(board);
        }
    }
#endif
}
