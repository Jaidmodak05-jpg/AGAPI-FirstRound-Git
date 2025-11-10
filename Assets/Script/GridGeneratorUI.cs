using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridGeneratorUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RectTransform board;       // parent for cards
    public GameObject cardPrefab;     // Card prefab (with CardUI)
    public Vector2 spacing = new Vector2(8f, 8f);
    public List<Sprite> faceSprites;  // 0..N-1 sprites

    public List<CardUI> Generate(int rows, int cols, System.Random rng)
    {
        if (!board || !cardPrefab)
        {
            Debug.LogError("GridGeneratorUI: assign Board and Card Prefab.");
            return new List<CardUI>();
        }

        // clear old
        for (int i = board.childCount - 1; i >= 0; i--)
            Destroy(board.GetChild(i).gameObject);

        int total = rows * cols;
        var result = new List<CardUI>(total);
        if (total <= 0) return result;

        // build ids: 0,0,1,1,2,2...
        int pairs = total / 2;
        var ids = new List<int>(total);
        for (int i = 0; i < pairs; i++) { ids.Add(i); ids.Add(i); }
        while (ids.Count < total) ids.Add(pairs);

        // shuffle
        for (int i = 0; i < ids.Count; i++)
        {
            int j = rng.Next(i, ids.Count);
            (ids[i], ids[j]) = (ids[j], ids[i]);
        }

        // compute auto size
        float size = 0f;
        {
            var rt = board.rect;
            float w = rt.width - (cols - 1) * spacing.x;
            float h = rt.height - (rows - 1) * spacing.y;
            size = Mathf.Floor(Mathf.Min(w / cols, h / rows));
        }

        float totalW = cols * size + (cols - 1) * spacing.x;
        float totalH = rows * size + (rows - 1) * spacing.y;
        float originX = -totalW * 0.5f + size * 0.5f;
        float originY = totalH * 0.5f - size * 0.5f;

        int k = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int id = ids[k++];
                var go = Instantiate(cardPrefab, board);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(size, size);

                float x = originX + c * (size + spacing.x);
                float y = originY - r * (size + spacing.y);
                rt.anchoredPosition = new Vector2(x, y);

                var card = go.GetComponent<CardUI>();

                // pick sprite safely
                Sprite face = (faceSprites != null && id >= 0 && id < faceSprites.Count)
                    ? faceSprites[id]
                    : null;

                card.Init(id, face);

                // link back to controller & audio
                var controller = FindObjectOfType<GameControllerUI>();
                if (controller)
                {
                    card.controller = controller;
                    card.audioMgr = controller.audioMgr;
                }

                result.Add(card);
            }

        return result;
    }
}
