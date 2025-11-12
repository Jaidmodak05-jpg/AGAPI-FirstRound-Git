using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public AudioManager audioMgr;   // ok if null

    [Header("Config (fallbacks when launching Game scene directly)")]
    public int rows = 4;
    public int cols = 4;
    public int startingTime = 30;   // will be overwritten by GameSettings

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text bestScoreText;
    public Button restartButton;          // hook in inspector
    public Button mainMenuFromGameOver;   // NEW: optional, hook if you add a Main Menu btn on the panel

    [Header("Pause UI (optional)")]
    public GameObject pausePanel;         // panel with Resume/Restart/Main Menu buttons (can be null)

    // --- runtime state ---
    private readonly List<CardUI> allCards = new();
    private readonly List<CardUI> pending = new();
    private bool resolving = false;

    private int score = 0;
    private int combo = 0;
    private int bestScore = 0;  // simple session best
    private int remainingCards = 0;
    private float timeLeft = 0f;
    private bool running = false;
    private bool inputLocked = false;
    private bool paused = false;

    // tuning
    private const int matchReward = 100;
    private const int missPenalty = 25; // score deduction on mismatch

    void Awake()
    {
        // Make sure we never enter the scene paused
        Time.timeScale = 1f;
    }

    void Start()
    {
        // Pull what the menu chose
        rows = GameSettings.Rows;
        cols = GameSettings.Cols;
        startingTime = GameSettings.StartingTime;

        // Optional wire-ups if you forgot to set them in the Inspector
        if (mainMenuFromGameOver)
            mainMenuFromGameOver.onClick.AddListener(OnMainMenuButton);

        StartNewGame(rows, cols);
    }

    public void StartNewGame(int r, int c)
    {
        rows = r;
        cols = c;

        // safety: if we restarted from pause, unpause
        Resume(silent: true);

        score = 0;
        combo = 0;
        timeLeft = startingTime;
        running = true;
        resolving = false;
        inputLocked = false;
        pending.Clear();

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);

        // Build grid
        if (grid)
        {
            allCards.Clear();
            allCards.AddRange(grid.Generate(rows, cols, new System.Random(), this, audioMgr));
        }

        remainingCards = allCards.Count;
        UpdateUI();
        UpdateTimerUI();
    }

    void Update()
    {
        // Keyboard toggle (Escape) if you like; or call OnTogglePauseKey from a UI button
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        if (!running || paused) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            running = false;
            ShowGameOver(false);
        }
        UpdateTimerUI();
    }

    // --------- Card events ----------
    public bool CanAcceptClick()
    {
        if (!running) return false;
        if (paused) return false;
        if (inputLocked) return false;
        if (resolving) return false;
        return true;
    }

    public void OnCardFlippedUp(CardUI card)
    {
        if (!running || paused) return;

        pending.Add(card);
        if (pending.Count == 2 && !resolving)
            StartCoroutine(ResolvePair());
    }

    IEnumerator ResolvePair()
    {
        resolving = true;

        var a = pending[0];
        var b = pending[1];

        // tiny delay so player sees both faces
        yield return new WaitForSeconds(0.25f);

        if (a.CardId == b.CardId)
        {
            // match!
            combo++;
            score += matchReward + (combo - 1) * 10; // small streak bonus
            if (audioMgr) audioMgr.Match(combo);

            yield return a.StartCoroutine(a.Vanish());
            yield return b.StartCoroutine(b.Vanish());

            remainingCards -= 2;

            // Win?
            if (remainingCards <= 0)
            {
                running = false;
                ShowGameOver(true);
            }
        }
        else
        {
            combo = 0;
            score = Mathf.Max(0, score - missPenalty);
            if (audioMgr) audioMgr.Miss();

            yield return a.StartCoroutine(a.FlipDown());
            yield return b.StartCoroutine(b.FlipDown());
        }

        pending.Clear();
        resolving = false;
        UpdateUI();
    }

    // --------- UI helpers ----------
    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (comboText) comboText.text = combo > 0 ? $"Combo: x{combo}" : "Combo: -";
    }

    void UpdateTimerUI()
    {
        if (!timerText) return;

        int t = Mathf.CeilToInt(timeLeft);
        int m = t / 60;
        int s = t % 60;
        timerText.text = $"{m:00}:{s:00}";
    }

    void ShowGameOver(bool won)
    {
        if (score > bestScore) bestScore = score;

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = won ? $"YOU WIN!\nScore: {score}" : $"Time’s up!\nScore: {score}";
        if (bestScoreText) bestScoreText.text = $"Best: {bestScore}";

        if (audioMgr)
        {
            if (won) audioMgr.Win();
            else audioMgr.GameOver();
        }
    }

    // --------- Buttons / external UI calls ----------
    public void OnRestartButton()
    {
        if (!running) // allow when panel is shown
            StartNewGame(rows, cols);
        else         // or from pause menu
            StartNewGame(rows, cols);
    }

    public void OnMainMenuButton()
    {
        // Always clear pause state/timescale before leaving
        Time.timeScale = 1f;
        paused = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnPauseButton() => TogglePause();
    public void OnResumeButton() => Resume();

    public void OnTogglePauseKey() => TogglePause(); // expose for UI if needed

    // --------- Pause logic ----------
    public void TogglePause()
    {
        if (!running) return; // no pausing on game over
        if (paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (paused) return;
        paused = true;
        Time.timeScale = 0f;
        inputLocked = true;
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void Resume(bool silent = false)
    {
        if (!paused && silent == false) { if (pausePanel) pausePanel.SetActive(false); }
        paused = false;
        Time.timeScale = 1f;
        inputLocked = false;
        if (pausePanel) pausePanel.SetActive(false);
    }
}
