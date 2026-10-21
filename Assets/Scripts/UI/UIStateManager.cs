using TMPro;
using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject startMenuPanel;
    [SerializeField] GameObject hudPanel;
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject gameOverPanel;

    [Header("HUD")]
    [Tooltip("Keep the HUD visible behind the pause menu.")]
    [SerializeField] bool showHudWhilePaused = true;

    [Header("Game Over (all optional)")]
    [SerializeField] TMP_Text finalScoreText;
    [SerializeField] TMP_Text highScoreText;
    [SerializeField] TMP_Text reasonText;
    [Tooltip("Shown only when the run set a new high score.")]
    [SerializeField] GameObject newHighScoreBadge;

    GameManager gm;

    void OnEnable()
    {
        gm = GameManager.Instance;
        if (gm == null)
        {
            Show(GameState.Running);
            return;
        }

        gm.OnStateChanged += Show;
        Show(gm.State);
    }

    void OnDisable()
    {
        if (gm != null)
            gm.OnStateChanged -= Show;
    }

    void Show(GameState state)
    {
        SetActive(startMenuPanel, state == GameState.Menu);
        SetActive(hudPanel, state == GameState.Running || (state == GameState.Paused && showHudWhilePaused));
        SetActive(pausePanel, state == GameState.Paused);
        SetActive(gameOverPanel, state == GameState.GameOver);

        if (state == GameState.GameOver)
            FillGameOver();
    }

    void FillGameOver()
    {
        if (gm == null) return;

        if (finalScoreText != null) finalScoreText.text = gm.Score.ToString();
        if (highScoreText != null) highScoreText.text = gm.HighScore.ToString();
        if (reasonText != null) reasonText.text = gm.GameOverReason;
        SetActive(newHighScoreBadge, gm.IsNewHighScore);
    }

    static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    public void OnPlayClicked()
    {
        if (gm != null) gm.StartRun();
    }

    public void OnResumeClicked()
    {
        if (gm != null) gm.Resume();
    }

    public void OnPauseClicked()
    {
        if (gm != null) gm.Pause();
    }

    public void OnRestartClicked()
    {
        if (gm != null) gm.Restart();
    }

    public void OnMainMenuClicked()
    {
        if (gm != null) gm.ReturnToMenu();
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}