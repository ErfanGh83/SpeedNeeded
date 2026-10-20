using UnityEngine;

public class Spinner : MonoBehaviour
{
    [Header("Spin")]
    [Tooltip("Degrees per second around each axis. (0, 120, 0) spins around the vertical axis.")]
    [SerializeField] Vector3 rotationSpeed = new Vector3(0f, 120f, 0f);
    [SerializeField] Space space = Space.Self;

    [Header("Bob (optional)")]
    [SerializeField] bool bob = false;
    [Tooltip("How far it moves up and down (world units).")]
    [SerializeField] float bobHeight = 0.2f;
    [SerializeField] float bobSpeed = 2f;
    [Tooltip("Start each object at a random point of the bob so a row of coins doesn't move in sync.")]
    [SerializeField] bool randomizePhase = true;

    [Header("Game state")]
    [Tooltip("Freeze while paused, in the menu or after game over.")]
    [SerializeField] bool onlyWhilePlaying = true;

    float phase;
    float appliedOffset;

    void OnEnable()
    {
        phase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        appliedOffset = 0f;
    }

    void OnDisable()
    {
        // Put the object back at its rest height before it goes back to the pool
        if (appliedOffset != 0f)
        {
            Vector3 p = transform.localPosition;
            p.y -= appliedOffset;
            transform.localPosition = p;
            appliedOffset = 0f;
        }
    }

    void Update()
    {
        if (onlyWhilePlaying && !GameManager.IsPlaying) return;

        transform.Rotate(rotationSpeed * Time.deltaTime, space);

        if (bob)
        {
            float offset = Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;

            Vector3 p = transform.localPosition;
            p.y += offset - appliedOffset; // apply only the change, so it never drifts
            transform.localPosition = p;

            appliedOffset = offset;
        }
    }
}