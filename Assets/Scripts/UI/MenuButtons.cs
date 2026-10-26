using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    [SerializeField] UIStateManager ui;

    public void Play()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartRun();
    }

    public void Resume()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();
    }

    public void Retry()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
    }

    public void MainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMenu();
    }

    public void OpenSettings()
    {
        if (ui != null) ui.OnSettingsClicked();
    }

    public void CloseSettings()
    {
        if (ui != null) ui.OnSettingsBackClicked();
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}