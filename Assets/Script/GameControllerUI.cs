using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameControllerUI : MonoBehaviour
{
    [Header("Refs")]
    public GridGeneratorUI grid;
    public RectTransform board;

    [Header("HUD (TextMeshPro)")]
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public TMP_Text timerText;

    [Header("Audio (optional)")]
    public AudioManager audioMgr;

    [Header("Config")]
    public int rows = 4;
    public int cols = 4;
    public int startingTime = 30; // seconds

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text bestScoreText;

    // --- runtime state ---
    readonly List<CardUI> allCards = new();
    readonly List<CardUI> pending = new();
    bool resolving = false;

    int score = 0;
    int combo = 0;
    int bestScore = 0;
    int remainingCards = 0;
    float timeLeft = 0f;
    bool running = false;

    void Start()
    {
        StartNewGame(rows, cols);
    }

    public void StartNewGame(int r, int c)
    {
        rows = r;
        cols = c;

        score = 0;
        combo = 0;
        timeLeft = startingTime;
        running = true;
        resolving = false;
        pending.Clear();

        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Build grid
        if (grid)
        {
            var rnd = new System.Random(); // different per run
            allCards.Clear();
            allCards.AddRange(grid.Generate(rows, cols, rnd, this, audioMgr));

            remainingCards = allCards.Count;
        }

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
            OnGameOver(false); // time out
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (comboText) comboText.text = combo > 0 ? $"Combo: x{combo}" : "Combo: -";
        if (timerText) timerText.text = timeLeft > 0 ? $"{timeLeft:00}" : "00";
    }

    // --- Input gating from CardUI ---
    public bool CanAcceptClick() => running && !resolving;

    public void OnCardFlippedUp(CardUI card)
    {
        if (!running || card.IsMatched) return;

        pending.Add(card);

        if (pending.Count == 2 && !resolving)
        {
            StartCoroutine(ResolvePair());
        }
        else if (pending.Count > 2)
        {
            // safety: if third somehow slips in, flip it back
            var extra = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            StartCoroutine(extra.FlipDown());
        }
    }

    IEnumerator ResolvePair()
    {
        resolving = true;

        var a = pending[0];
        var b = pending[1];

        // tiny delay so player can see
        yield return new WaitForSeconds(0.25f);

        if (a.CardId == b.CardId)
        {
            // MATCH
            combo++;
            score += 100 * combo;
            audioMgr?.Match(combo);

            yield return StartCoroutine(a.Vanish());
            yield return StartCoroutine(b.Vanish());

            remainingCards -= 2;

            if (remainingCards <= 0)
            {
                running = false;
                OnGameOver(true); // win
            }
        }
        else
        {
            // MISS
            combo = 0;
            audioMgr?.Miss();

            yield return StartCoroutine(a.FlipDown());
            yield return StartCoroutine(b.FlipDown());
        }

        pending.Clear();
        resolving = false;
        UpdateUI();
    }

    // --- End states ---
    void OnGameOver(bool won)
    {
        if (won) audioMgr?.Win(); else audioMgr?.GameOver();

        // Best score
        if (score > bestScore) bestScore = score;

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = won ? $"You Win!\nScore: {score}" : $"Time Up!\nScore: {score}";
        if (bestScoreText) bestScoreText.text = $"Best: {bestScore}";
    }

    // Hook this to your Restart button
    public void OnRestartButton()
    {
        StartNewGame(rows, cols);
    }
}
