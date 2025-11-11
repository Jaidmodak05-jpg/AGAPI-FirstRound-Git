using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameControllerUI : MonoBehaviour
{
    [Header("Refs")]
    public GridGeneratorUI grid;        // your spawner
    public RectTransform board;         // parent that holds the spawned cards

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public TMP_Text timerText;

    [Header("Audio (optional)")]
    public AudioManager audioMgr;       // ok if left null

    [Header("Config")]
    public int rows = 4;
    public int cols = 4;
    public float startingTime = 90f;    // seconds

    [Header("Game Over UI")]
    public GameObject gameOverPanel;    // panel with results
    public TMP_Text finalScoreText;
    public TMP_Text bestScoreText;

    // --- runtime state ---
    int score = 0;
    int combo = 0;
    float timeLeft = 0f;
    bool running = false;

    // click gating
    bool resolving = false;             // resolving a pair
    bool inputLocked = false;           // brief lock while showing 2nd card
    readonly List<CardUI> pending = new();

    int bestScore;

    void Start()
    {
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        StartNewGame(rows, cols);
    }

    // Start or restart a run
    public void StartNewGame(int r, int c)
    {
        rows = r;
        cols = c;

        score = 0;
        combo = 0;
        timeLeft = startingTime;
        running = true;
        resolving = false;
        inputLocked = false;
        pending.Clear();

        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Build grid
        if (grid)
            grid.Generate(rows, cols, new System.Random());

        UpdateUI();
    }

    void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            running = false;

            // Time-up (lose)
            if (audioMgr) audioMgr.GameOver();
            FinishRun(false);
        }

        UpdateUI();
    }

    void FinishRun(bool won)
    {
        // Persist best score
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        if (finalScoreText)
        {
            finalScoreText.text = won ? $"You Win!  Score: {score}"
                                      : $"Time's Up  Score: {score}";
        }
        if (bestScoreText) bestScoreText.text = $"Best: {bestScore}";
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (comboText) comboText.text = combo > 0 ? $"Combo: x{combo}" : "Combo: -";

        if (timerText)
        {
            timerText.text = FormatTime(timeLeft);

            // subtle urgency color shift under 20s
            float t = Mathf.InverseLerp(20f, 0f, timeLeft);
            timerText.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), t);
        }
    }

    static string FormatTime(float t)
    {
        t = Mathf.Max(0f, t);
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        return $"{m:00}:{s:00}";
    }

    // ===== CLICK FLOW =====

    // Cards call this before accepting a click
    public bool CanAcceptClick()
    {
        return running && !resolving && !inputLocked;
    }

    // Cards notify here after they flip up
    public void OnCardFlippedUp(CardUI card)
    {
        if (!running || resolving || inputLocked) return;
        if (pending.Count == 1 && ReferenceEquals(pending[0], card)) return; // ignore double

        pending.Add(card);

        if (pending.Count == 2)
        {
            // lock now so a 3rd click can't sneak in during the small reveal delay
            inputLocked = true;
            resolving = true;
            StartCoroutine(ResolvePair());
        }
    }

    IEnumerator ResolvePair()
    {
        // brief reveal so player sees both faces
        yield return new WaitForSeconds(0.25f);

        var a = pending[0];
        var b = pending[1];

        if (a.CardId == b.CardId)
        {
            // MATCH
            combo++;
            score += 100 * combo;
            if (audioMgr) audioMgr.Match(combo);

            a.SetMatched(); b.SetMatched();
            yield return StartCoroutine(a.Vanish());
            yield return StartCoroutine(b.Vanish());

            // Win when all cards under 'board' are matched (we keep slots active)
            if (AllCardsMatched())
            {
                running = false;
                if (audioMgr) audioMgr.Win();   // NEW: distinct win sound
                FinishRun(true);
            }
        }
        else
        {
            // MISS
            combo = 0;
            score = Mathf.Max(0, score - 20);
            if (audioMgr) audioMgr.Miss();

            yield return StartCoroutine(a.FlipDown());
            yield return StartCoroutine(b.FlipDown());
        }

        pending.Clear();
        resolving = false;
        inputLocked = false;
        UpdateUI();
    }

    bool AllCardsMatched()
    {
        if (!board) return false;

        // We don’t rely on activeSelf (cards remain active to keep their grid slots).
        for (int i = 0; i < board.childCount; i++)
        {
            var cu = board.GetChild(i).GetComponent<CardUI>();
            if (cu && !cu.IsMatched) return false;
        }
        return true;
    }

    // UI Button hook
    public void OnRestartButton()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        StartNewGame(rows, cols);
    }
}
