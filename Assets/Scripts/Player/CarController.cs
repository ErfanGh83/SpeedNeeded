using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 3-lane car controller (event-driven input).
/// The Move action changes the target lane; Update only slides the car's x toward it.
/// Lane 0 = left, 1 = middle, 2 = right.
/// </summary>
public class CarController : MonoBehaviour
{
    [Header("Lanes")]
    [Tooltip("Distance between two neighbouring lanes (world units).")]
    [SerializeField] float laneDistance = 3f;

    [Tooltip("Horizontal offset of the middle lane (use if your road isn't centered at x = 0).")]
    [SerializeField] float centerOffset = 0f;

    [Tooltip("0 = left, 1 = middle, 2 = right")]
    [SerializeField, Range(0, 2)] int startLane = 1;

    [Header("Movement")]
    [Tooltip("How fast the car slides between lanes (units per second). Higher = snappier.")]
    [SerializeField] float laneChangeSpeed = 12f;

    [Header("Input")]
    [Tooltip("How far the Move axis must be pushed to count as a lane change (matters for gamepad sticks).")]
    [SerializeField, Range(0.1f, 1f)] float inputThreshold = 0.5f; 

    public event Action OnTurnLeft;
    public event Action OnTurnRight;

    const int LeftLane = 0;
    const int RightLane = 2;

    InputSystem_Actions input;
    int currentLane;
    int lastInputDir; // so holding a key only changes lane once

    public int CurrentLane => currentLane;

    void Awake()
    {
        input = new InputSystem_Actions();
        currentLane = startLane;

        // Snap to the starting lane
        Vector3 pos = transform.localPosition;
        pos.x = GetLaneX(currentLane);
        transform.localPosition = pos;
    }

    void OnEnable()
    {
        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMoveCanceled;
        input.Player.Enable();
    }

    void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMoveCanceled;
        input.Player.Disable();
        lastInputDir = 0;
    }

    void OnDestroy()
    {
        input.Dispose();
    }

    void Update()
    {
        MoveToLane(); // only the smooth movement lives here
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        if (!GameManager.IsPlaying) return;
        float x = ctx.ReadValue<Vector2>().x;

        int dir = 0;
        if (x <= -inputThreshold) dir = -1;
        else if (x >= inputThreshold) dir = 1;

        // React only when the direction changes (ignores held keys and re-fires from W/S)
        if (dir != 0 && dir != lastInputDir)
            ChangeLane(dir);

        lastInputDir = dir;
    }

    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        lastInputDir = 0;
    }

    void ChangeLane(int dir)
    {
        int target = Mathf.Clamp(currentLane + dir, LeftLane, RightLane);
        if (target == currentLane) return; // already at the edge

        currentLane = target;

        if (dir < 0) OnTurnLeft?.Invoke();
        else OnTurnRight?.Invoke();
    }

    void MoveToLane()
    {
        float targetX = GetLaneX(currentLane);
        Vector3 pos = transform.localPosition;
        if (Mathf.Approximately(pos.x, targetX)) return;

        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.deltaTime);
        transform.localPosition = pos;
    }

    // Lane 1 sits at centerOffset, lanes 0 and 2 are one laneDistance away on each side.
    // Read every frame, so you can tune laneDistance live in the inspector.
    public float GetLaneX(int lane)
    {
        return centerOffset + (lane - 1) * laneDistance;
    }

#if UNITY_EDITOR
    // Draws the three lane positions in the Scene view so you can tune the distance visually
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Transform parent = transform.parent;
        for (int i = 0; i < 3; i++)
        {
            Vector3 local = new Vector3(GetLaneX(i), transform.localPosition.y, transform.localPosition.z);
            Vector3 world = parent != null ? parent.TransformPoint(local) : local;
            Gizmos.DrawWireCube(world, new Vector3(0.5f, 0.1f, 2f));
        }
    }
#endif
}
