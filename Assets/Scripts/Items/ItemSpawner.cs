using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public enum Need { None, Fuel, Health }

    [System.Serializable]
    public class ItemEntry
    {
        public string label = "Item";
        public ObjectPool pool;

        [Tooltip("Higher = spawns more often compared to the other entries.")]
        public float weight = 1f;

        [Tooltip("How many spawn in one group (min, max inclusive). Coins: 3-6. Fuel/heal: 1-1.")]
        public Vector2Int groupSize = new Vector2Int(1, 1);

        [Tooltip("Distance between the items of a group along the road.")]
        public float spacing = 3f;

        [Header("Need-based (fuel / heal)")]
        public Need need = Need.None;

        [Tooltip("Weight multiplier when the resource is empty. Fades to x1 when it is full.")]
        public float lowBoost = 3f;

        [Tooltip("Never spawn this item while the resource is full.")]
        public bool skipWhenFull = true;
    }

    [Header("References")]
    [Tooltip("The player's CarController: lane positions come from it.")]
    [SerializeField] CarController laneSource;
    [Tooltip("Optional: found automatically on the Lane Source object if left empty.")]
    [SerializeField] FuelSystem fuel;
    [SerializeField] HealthSystem health;
    [SerializeField] List<ItemEntry> entries = new List<ItemEntry>();

    [Header("Spawning")]
    [Tooltip("Random time between spawn attempts (seconds).")]
    [SerializeField] Vector2 spawnInterval = new Vector2(2f, 4f);

    [Tooltip("Distance an item travels before it goes back to the pool. Spawn distance to the player + a margin.")]
    [SerializeField] float despawnDistance = 100f;

    [Header("Avoid overlapping traffic")]
    [SerializeField] bool avoidOverlap = true;
    [Tooltip("Half size of the box checked at the spawn point for cars/items already there.")]
    [SerializeField] Vector3 overlapHalfExtents = new Vector3(1f, 1f, 2f);

    [Header("Debug")]
    [Tooltip("Logs run-state changes, spawn attempts, and reasons an item was skipped.")]
    [SerializeField] bool logSpawns = true;

    float nextSpawnTime;
    bool wasPlaying;

    void Start()
    {
        if (laneSource == null)
        {
            Debug.LogError("ItemSpawner: assign the player's CarController as Lane Source.", this);
            enabled = false;
            return;
        }

        if (fuel == null) fuel = laneSource.GetComponent<FuelSystem>();
        if (health == null) health = laneSource.GetComponent<HealthSystem>();

        for (int i = 0; i < entries.Count; i++)
        {
            ItemEntry e = entries[i];
            if (e.pool == null) Debug.LogWarning($"ItemSpawner: entry {i} ({e.label}) has no Pool assigned.", this);
            else if (e.weight <= 0f) Debug.LogWarning($"ItemSpawner: entry {i} ({e.label}) has Weight {e.weight}.", this);
            else if (e.need == Need.Fuel && fuel == null) Debug.LogWarning($"ItemSpawner: entry {i} ({e.label}) needs a FuelSystem on the player.", this);
            else if (e.need == Need.Health && health == null) Debug.LogWarning($"ItemSpawner: entry {i} ({e.label}) needs a HealthSystem on the player.", this);
        }

        nextSpawnTime = Time.time + 1.5f;
        wasPlaying = GameManager.IsPlaying;
        if (logSpawns)
            Debug.Log($"[ItemSpawner] Ready: {entries.Count} entries, playing={wasPlaying}, first attempt in 1.5s.", this);
    }

    void Update()
    {
        bool isPlaying = GameManager.IsPlaying;
        if (isPlaying != wasPlaying)
        {
            wasPlaying = isPlaying;
            if (logSpawns) Debug.Log($"[ItemSpawner] Game playing state changed to {isPlaying}.", this);
        }

        if (!isPlaying) return; // no spawning in menu, pause or game over

        if (Time.time < nextSpawnTime) return;
        nextSpawnTime = Time.time + Random.Range(spawnInterval.x, spawnInterval.y);

        TrySpawn();
    }

    void TrySpawn()
    {
        if (logSpawns) Debug.Log("[ItemSpawner] Attempting item spawn.", this);

        ItemEntry entry = PickEntry();
        if (entry == null)
        {
            if (logSpawns) Debug.Log("[ItemSpawner] No eligible item entry. Check the entry pools, weights, and need/full settings.", this);
            return;
        }

        int lane = Random.Range(0, 3);
        Vector3 first = new Vector3(laneSource.GetLaneX(lane), transform.position.y, transform.position.z);

        // Don't drop pickups onto a car that just spawned in this lane
        if (avoidOverlap && IsBlocked(first))
        {
            if (logSpawns) Debug.Log($"[ItemSpawner] Lane {lane} is occupied, skipped {entry.label}", this);
            return;
        }

        int count = Random.Range(entry.groupSize.x, entry.groupSize.y + 1);
        if (count <= 0)
        {
            Debug.LogWarning($"[ItemSpawner] '{entry.label}' has an invalid group size {entry.groupSize}; no items spawned.", this);
            return;
        }

        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            float offset = i * entry.spacing;
            Vector3 position = first + Vector3.forward * offset; // further from the player (objects move toward -Z)

            GameObject obj = entry.pool.Get(position, transform.rotation);
            if (obj == null)
            {
                Debug.LogWarning($"[ItemSpawner] Pool '{entry.pool.name}' returned nothing for '{entry.label}'. Check its Prefabs list and Can Grow setting.", entry.pool);
                break;
            }

            if (obj.TryGetComponent(out Interactable interactable))
            {
                // Speed 0 = fixed to the road. Later items start further away, so they travel a bit further.
                interactable.Initialize(0f, despawnDistance + offset);
                spawned++;
            }
            else
            {
                Debug.LogWarning($"ItemSpawner: '{obj.name}' has no Interactable script (Coin/FuelCan/HealthPickup).", obj);
            }
        }

        if (logSpawns)
        {
            if (spawned > 0) Debug.Log($"[ItemSpawner] Spawned {entry.label} x{spawned} in lane {lane} at {first}.", this);
            else Debug.LogWarning($"[ItemSpawner] Spawn attempt for '{entry.label}' produced no usable Interactable items.", this);
        }
    }

    bool IsBlocked(Vector3 position)
    {
        Collider[] hits = Physics.OverlapBox(position, overlapHalfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider c in hits)
            if (c.GetComponentInParent<Interactable>() != null) return true;

        return false;
    }

    // Base weight, boosted when the player needs the resource, zero when it's full
    float GetWeight(ItemEntry e)
    {
        if (e.pool == null || e.weight <= 0f) return 0f;
        if (e.need == Need.None) return e.weight;

        float level = 1f;
        if (e.need == Need.Fuel && fuel != null) level = fuel.Normalized;
        else if (e.need == Need.Health && health != null) level = health.Normalized;

        if (e.skipWhenFull && level >= 0.999f) return 0f;

        return e.weight * Mathf.Lerp(e.lowBoost, 1f, level);
    }

    ItemEntry PickEntry()
    {
        float total = 0f;
        foreach (ItemEntry e in entries) total += GetWeight(e);
        if (total <= 0f)
        {
            if (logSpawns)
            {
                foreach (ItemEntry e in entries)
                {
                    string reason = GetUnavailableReason(e);
                    Debug.Log($"[ItemSpawner] Entry '{e.label}' unavailable: {reason}.", this);
                }
            }
            return null;
        }

        float roll = Random.value * total;
        ItemEntry last = null;

        foreach (ItemEntry e in entries)
        {
            float w = GetWeight(e);
            if (w <= 0f) continue;

            last = e;
            roll -= w;
            if (roll <= 0f) return e;
        }

        return last; // floating point safety net
    }

    string GetUnavailableReason(ItemEntry e)
    {
        if (e.pool == null) return "pool not assigned";
        if (e.weight <= 0f) return $"weight is {e.weight}";
        if (e.need == Need.Fuel && fuel == null) return "player has no FuelSystem";
        if (e.need == Need.Health && health == null) return "player has no HealthSystem";

        float level = e.need == Need.Fuel ? fuel.Normalized :
            e.need == Need.Health ? health.Normalized : 0f;
        if (e.need != Need.None && e.skipWhenFull && level >= 0.999f)
            return $"resource is full ({level:P0}) and Skip When Full is enabled";

        return "entry has no usable weight";
    }

#if UNITY_EDITOR
    // Shows the three spawn points in the Scene view
    void OnDrawGizmosSelected()
    {
        if (laneSource == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = new Vector3(laneSource.GetLaneX(i), transform.position.y, transform.position.z);
            Gizmos.DrawWireCube(p, overlapHalfExtents * 2f);
        }
    }
#endif
}
