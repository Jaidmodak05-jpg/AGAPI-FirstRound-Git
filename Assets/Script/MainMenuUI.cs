using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Custom Panel (optional)")]
    public GameObject customPanel;
    public TMP_InputField rowsInput;
    public TMP_InputField colsInput;
    public TMP_InputField timeInput;

    [Header("Scene To Load")]
    public string gameSceneName = "Game";   // <-- make sure this matches your scene name

    void Awake()
    {
        if (customPanel) customPanel.SetActive(false);
        // sensible defaults if player launches directly
        GameSettings.Rows = 4;
        GameSettings.Cols = 4;
        GameSettings.StartingTime = 90;
    }

    // Presets
    public void OnEasy() { StartPreset(2, 2, 15); }
    public void OnNormal() { StartPreset(3, 4, 30); }
    public void OnHard() { StartPreset(5, 6, 60); }

    void StartPreset(int r, int c, int t)
    {
        GameSettings.Rows = r;
        GameSettings.Cols = c;
        GameSettings.StartingTime = t;
        // load by name (make sure it’s "Game" in Build Settings)
        SceneManager.LoadScene(gameSceneName);
        // Alternatively, load by build index: SceneManager.LoadScene(1);
    }

    // Custom dialog
    public void OnCustomOpen()
    {
        if (!customPanel) return;
        customPanel.SetActive(true);
        if (rowsInput) rowsInput.text = GameSettings.Rows.ToString();
        if (colsInput) colsInput.text = GameSettings.Cols.ToString();
        if (timeInput) timeInput.text = GameSettings.StartingTime.ToString();
    }

    public void OnCustomApply()
    {
        int r = ParseSafe(rowsInput?.text, 4);
        int c = ParseSafe(colsInput?.text, 4);
        int t = ParseSafe(timeInput?.text, 90);
        GameSettings.Rows = Mathf.Max(2, r);
        GameSettings.Cols = Mathf.Max(2, c);
        GameSettings.StartingTime = Mathf.Max(10, t);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnCustomCancel() { if (customPanel) customPanel.SetActive(false); }
    public void OnQuit() { Application.Quit(); }

    int ParseSafe(string s, int fallback) => int.TryParse(s, out var v) ? v : fallback;
}
