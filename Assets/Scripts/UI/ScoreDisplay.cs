using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text highScoreText;

    [SerializeField] string scoreFormat = "{0}";
    [SerializeField] string highScoreFormat = "Best: {0}";
    [Tooltip("ON: the best score rises live as you pass it. OFF: it updates only at game over.")]
    [SerializeField] bool liveHighScore = true;

    GameManager game;

    void OnEnable()
    {
        game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogWarning("ScoreDisplay: no GameManager in the scene.", this);
            return;
        }

        game.OnScoreChanged += OnScoreChanged;
        game.OnStateChanged += OnStateChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (game == null) return;

        game.OnScoreChanged -= OnScoreChanged;
        game.OnStateChanged -= OnStateChanged;
        game = null;
    }

    void OnScoreChanged(int score) => Refresh();
    void OnStateChanged(GameState state) => Refresh();

    void Refresh()
    {
        if (game == null) return;

        int score = game.Score;
        int best = liveHighScore ? Mathf.Max(game.HighScore, score) : game.HighScore;

        if (scoreText != null) scoreText.text = string.Format(scoreFormat, score);
        if (highScoreText != null) highScoreText.text = string.Format(highScoreFormat, best);
    }
}