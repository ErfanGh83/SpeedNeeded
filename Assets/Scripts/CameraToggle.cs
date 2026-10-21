using UnityEngine;

public class CameraToggle : MonoBehaviour
{
    [SerializeField] GameObject menuCamera;
    [SerializeField] GameObject playCamera;

    GameManager gm;

    void OnEnable()
    {
        gm = GameManager.Instance;
        if (gm == null)
        {
            Apply(GameState.Running);
            return;
        }

        gm.OnStateChanged += Apply;
        Apply(gm.State);
    }

    void OnDisable()
    {
        if (gm != null)
            gm.OnStateChanged -= Apply;
    }

    void Apply(GameState state)
    {
        bool menu = state == GameState.Menu;

        if (menuCamera != null) menuCamera.SetActive(menu);
        if (playCamera != null) playCamera.SetActive(!menu);
    }
}