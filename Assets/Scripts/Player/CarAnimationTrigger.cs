using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarAnimationTrigger : MonoBehaviour
{
    [Tooltip("Leave empty to auto-find an Animator on this object or its children.")]
    [SerializeField] Animator animator;

    [SerializeField] string turnLeftTrigger = "TurnLeft";
    [SerializeField] string turnRightTrigger = "TurnRight";

    CarController car;
    int turnLeftHash;
    int turnRightHash;

    void Awake()
    {
        car = GetComponent<CarController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        turnLeftHash = Animator.StringToHash(turnLeftTrigger);
        turnRightHash = Animator.StringToHash(turnRightTrigger);
    }

    void OnEnable()
    {
        car.OnTurnLeft += HandleTurnLeft;
        car.OnTurnRight += HandleTurnRight;
    }

    void OnDisable()
    {
        car.OnTurnLeft -= HandleTurnLeft;
        car.OnTurnRight -= HandleTurnRight;
    }

    void HandleTurnLeft()
    {
        if (animator == null) return;
        animator.ResetTrigger(turnRightHash); // avoid a stale opposite trigger firing afterwards
        animator.SetTrigger(turnLeftHash);
    }

    void HandleTurnRight()
    {
        if (animator == null) return;
        animator.ResetTrigger(turnLeftHash);
        animator.SetTrigger(turnRightHash);
    }
}