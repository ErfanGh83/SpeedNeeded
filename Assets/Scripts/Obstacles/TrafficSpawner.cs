using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public ObjectPool pool;

        [Tooltip("Higher = spawns more often compared to the other entries.")]
        public float weight = 1f;

        [Tooltip("Random own speed (units/s). Cars driving the same way should be slower than World Speed. Use 0 for things fixed to the road (coins).")]
        public Vector2 speedRange = new Vector2(4f, 10f);
    }

    [Header("References")]
    [Tooltip("The player's CarController: the spawner reads the lane positions from it.")]
    [SerializeField] CarController laneSource;
    [SerializeField] List<SpawnEntry> entries = new List<SpawnEntry>();

    [Header("Spawning")]
    [Tooltip("Random time between spawn attempts (seconds).")]
    [SerializeField] Vector2 spawnInterval = new Vector2(0.8f, 2f);

    [Tooltip("After spawning in a lane, that lane is skipped for this long (seconds). Stops cars piling up.")]
    [SerializeField] float laneCooldown = 1.5f;

    [Tooltip("Distance an object travels before it goes back to the pool. Use spawn distance to the player + a margin.")]
    [SerializeField] float despawnDistance = 100f;

    readonly float[] laneBlockedUntil = new float[3];
    readonly List<int> freeLanes = new List<int>(3);
    float nextSpawnTime;

    void Start()
    {
        if (laneSource == null)
        {
            Debug.LogError("TrafficSpawner: assign the player's CarController as Lane Source.", this);
            enabled = false;
            return;
        }

        nextSpawnTime = Time.time + 1f;
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return; // no spawning in menu, pause or game over

        if (Time.time < nextSpawnTime) return;
        nextSpawnTime = Time.time + Random.Range(spawnInterval.x, spawnInterval.y);

        TrySpawn();
    }

    void TrySpawn()
    {
        freeLanes.Clear();
        for (int i = 0; i < 3; i++)
            if (Time.time >= laneBlockedUntil[i]) freeLanes.Add(i);

        // Never block all three lanes at once: the player must always have a way through
        if (freeLanes.Count < 2) return;

        SpawnEntry entry = PickEntry();
        if (entry == null)
        {
            Debug.LogWarning("TrafficSpawner: no usable entry. Add an entry and assign its Pool.", this);
            return;
        }

        int lane = freeLanes[Random.Range(0, freeLanes.Count)];
        Vector3 position = new Vector3(laneSource.GetLaneX(lane), transform.position.y, transform.position.z);

        GameObject obj = entry.pool.Get(position, transform.rotation);
        if (obj == null)
        {
            Debug.LogWarning($"TrafficSpawner: pool '{entry.pool.name}' returned nothing. Is its Prefabs list filled in?", this);
            return;
        }

        if (obj.TryGetComponent(out Interactable interactable))
        {
            float speed = Random.Range(entry.speedRange.x, entry.speedRange.y);
            interactable.Initialize(speed, despawnDistance);
        }
        else
        {
            Debug.LogWarning($"TrafficSpawner: '{obj.name}' has no Interactable (TrafficCar/Coin) script, so it won't move.", obj);
        }

        laneBlockedUntil[lane] = Time.time + laneCooldown;
    }

    // Weighted random choice between the entries
    SpawnEntry PickEntry()
    {
        float total = 0f;
        foreach (SpawnEntry e in entries)
            if (e.pool != null) total += e.weight;

        if (total <= 0f) return null;

        float roll = Random.value * total;
        SpawnEntry last = null;

        foreach (SpawnEntry e in entries)
        {
            if (e.pool == null) continue;
            last = e;
            roll -= e.weight;
            if (roll <= 0f) return e;
        }

        return last; // floating point safety net
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (laneSource == null) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = new Vector3(laneSource.GetLaneX(i), transform.position.y, transform.position.z);
            Gizmos.DrawWireCube(p, new Vector3(1f, 0.5f, 2f));
        }
    }
#endif
}
