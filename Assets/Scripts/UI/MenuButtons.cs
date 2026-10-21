using UnityEngine;

public class MenuButtons : MonoBehaviour
{
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

    public void Restart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMenu();
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