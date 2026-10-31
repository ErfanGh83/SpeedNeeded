using System.Collections.Generic;
using UnityEngine;

public class TrafficCar : Interactable
{
    enum Phase { Idle, Warning, Changing }

    [Header("Lane changing")]
    [Tooltip("Chance (0-1) that a spawned car is a lane changer. Use 0 on a prefab for cars that always stay in one lane.")]
    [SerializeField, Range(0f, 1f)] float changeChance = 0.35f;

    [Tooltip("Random wait between lane changes (seconds).")]
    [SerializeField] Vector2 timeBetweenChanges = new Vector2(1.5f, 4f);

    [Tooltip("Pause before moving, so the player can see it coming (seconds). Hook a blinker here later.")]
    [SerializeField, Min(0f)] float warningTime = 0.4f;

    [Tooltip("Sideways speed (units/s).")]
    [SerializeField] float laneChangeSpeed = 4f;

    [Tooltip("Won't change into a lane if another car is closer than this (along the road).")]
    [SerializeField] float clearDistance = 10f;

    [Tooltip("Won't change lanes if that would put cars in all three lanes side by side within this distance.")]
    [SerializeField] float wallDistance = 14f;

    [Tooltip("Within this distance of the player the car stops changing lanes.")]
    [SerializeField] float stopNearPlayerDistance = 30f;

    static readonly List<TrafficCar> active = new List<TrafficCar>();
    static readonly int[] candidates = new int[2];
    static CarController laneSource;

    Phase phase;
    int currentLane;
    int targetLane;
    bool canChange;
    bool needsSetup;
    float timer;

    void OnEnable()
    {
        active.Add(this);
        needsSetup = true;
    }

    void OnDisable()
    {
        active.Remove(this);
    }

    protected override void Update()
    {
        base.Update(); // forward movement and despawn

        if (!GameManager.IsPlaying) return;

        if (laneSource == null)
        {
            laneSource = FindAnyObjectByType<CarController>();
            if (laneSource == null) return;
        }

        if (needsSetup) Setup();

        if (!canChange) return;

        switch (phase)
        {
            case Phase.Idle:
                timer -= Time.deltaTime;
                if (timer <= 0f) TryStartChange();
                break;

            case Phase.Warning:
                timer -= Time.deltaTime;
                if (timer <= 0f) phase = Phase.Changing;
                break;

            case Phase.Changing:
                MoveToTargetLane();
                break;
        }
    }

    void Setup()
    {
        needsSetup = false;

        // The spawner puts the car exactly on a lane, so find the closest one
        float x = transform.position.x;
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            float d = Mathf.Abs(laneSource.GetLaneX(i) - x);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        currentLane = best;
        targetLane = best;
        phase = Phase.Idle;
        canChange = Random.value < changeChance;
        timer = Random.Range(timeBetweenChanges.x, timeBetweenChanges.y);
    }

    void TryStartChange()
    {
        float distanceToPlayer = Mathf.Abs(transform.position.z - laneSource.transform.position.z);
        if (distanceToPlayer < stopNearPlayerDistance)
        {
            canChange = false;
            return;
        }

        int count = 0;
        if (currentLane > 0) candidates[count++] = currentLane - 1;
        if (currentLane < 2) candidates[count++] = currentLane + 1;

        if (count == 2 && Random.value < 0.5f)
        {
            int tmp = candidates[0];
            candidates[0] = candidates[1];
            candidates[1] = tmp;
        }

        for (int i = 0; i < count; i++)
        {
            if (!IsLaneClear(candidates[i]) || WouldFormWall(candidates[i])) continue;

            targetLane = candidates[i];
            phase = Phase.Warning;
            timer = warningTime;
            return;
        }

        timer = 0.5f; // no free lane right now, try again soon
    }

    bool IsLaneClear(int lane)
    {
        float z = transform.position.z;

        foreach (TrafficCar other in active)
        {
            if (other == this) continue;
            if (other.currentLane != lane && other.targetLane != lane) continue;
            if (Mathf.Abs(other.transform.position.z - z) < clearDistance) return false;
        }

        return true;
    }

    bool WouldFormWall(int lane)
    {
        float z = transform.position.z;

        for (int l = 0; l < 3; l++)
        {
            if (l == lane) continue;

            bool occupied = false;
            foreach (TrafficCar other in active)
            {
                if (other == this) continue;
                if (other.currentLane != l && other.targetLane != l) continue;
                if (Mathf.Abs(other.transform.position.z - z) < wallDistance)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied) return false; // at least one lane stays open
        }

        return true;
    }

    void MoveToTargetLane()
    {
        Vector3 pos = transform.position;
        float targetX = laneSource.GetLaneX(targetLane);
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Approximately(pos.x, targetX))
        {
            currentLane = targetLane;
            phase = Phase.Idle;
            timer = Random.Range(timeBetweenChanges.x, timeBetweenChanges.y);
        }
    }

    protected override void OnInteract(CarController player)
    {
        HealthSystem health = player.GetComponent<HealthSystem>();

        if (health != null) health.TakeDamage(1);
        else if (GameManager.Instance != null) GameManager.Instance.GameOver("Crashed into a car");
    }
}