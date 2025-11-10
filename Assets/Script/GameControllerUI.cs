using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameControllerUI : MonoBehaviour
{
    [Header("Refs")]
    public GridGeneratorUI grid;
    public RectTransform board;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public TMP_Text timerText;

    [Header("Audio")]
    public AudioManager audioMgr;

    [Header("Config")]
    public int rows = 4;
    public int cols = 4;
    public float startingTime = 90f;

    [Header("Game Over UI (optional)")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text bestScoreText;

    // runtime
    int score;
    int combo;
    float timeLeft;
    bool running;

    // input gating
    bool resolving = false;       // we're resolving a pair (no new clicks)
    bool inputLocked = false;     // global click gate while resolving

    readonly List<CardUI> pending = new();

    int bestScore;

    void Start()
    {
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        StartNewGame(rows, cols);
    }

    public void StartNewGame(int r, int c)
    {
        rows = r; cols = c;

        score = 0;
        combo = 0;
        timeLeft = startingTime;
        running = true;
        resolving = false;
        inputLocked = false;
        pending.Clear();

        if (gameOverPanel) gameOverPanel.SetActive(false);

        grid.Generate(rows, cols, new System.Random());   // your spawner
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
            if (audioMgr) audioMgr.GameOver();
            FinishRun();
        }

        UpdateUI();
    }

    void FinishRun()
    {
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        if (finalScoreText) finalScoreText.text = $"Score: {score}";
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
            var t = Mathf.InverseLerp(20f, 0f, timeLeft);
            timerText.color = Color.Lerp(Color.white, new Color(1f, .3f, .3f), t);
        }
    }

    string FormatTime(float t)
    {
        t = Mathf.Max(0f, t);
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        return $"{m:00}:{s:00}";
    }

    // ====== CLICK FLOW ======

    // Card asks: may I accept a click right now?
    public bool CanAcceptClick()
    {
        return running && !resolving && !inputLocked;
    }

    // Card informs: I have just flipped up and am ready to be evaluated.
    public void OnCardFlippedUp(CardUI card)
    {
        if (!running || resolving || inputLocked) return;
        if (pending.Count == 1 && ReferenceEquals(pending[0], card)) return; // ignore double

        pending.Add(card);

        if (pending.Count == 2)
        {
            // lock immediately to prevent a 3rd click during the 0.25s reveal
            inputLocked = true;
            resolving = true;
            StartCoroutine(ResolvePair());
        }
    }

    IEnumerator ResolvePair()
    {
        // small reveal delay
        yield return new WaitForSeconds(0.25f);

        var a = pending[0];
        var b = pending[1];

        if (a.CardId == b.CardId)
        {
            combo++;
            score += 100 * combo;
            if (audioMgr) audioMgr.Match(combo);

            a.SetMatched(); b.SetMatched();
            yield return StartCoroutine(a.Vanish());
            yield return StartCoroutine(b.Vanish());

            if (AllCardsGone())
            {
                running = false;
                if (audioMgr) audioMgr.GameOver();
                FinishRun();
            }
        }
        else
        {
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

    bool AllCardsGone()
    {
        if (!board) return false;
        for (int i = 0; i < board.childCount; i++)
            if (board.GetChild(i).gameObject.activeSelf)
                return false;
        return true;
    }

    // UI Button
    public void OnRestartButton()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        StartNewGame(rows, cols);
    }
}
