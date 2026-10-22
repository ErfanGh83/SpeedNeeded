using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BuildingSpawner : MonoBehaviour
{
    [System.Serializable]
    class Side
    {
        public bool enabled = true;
        [Tooltip("-1 = left of the road, +1 = right of the road.")]
        public int direction = -1;
        [Tooltip("Y rotation of the buildings on this side (e.g. 0 on the left, 180 on the right so they face the road).")]
        public float yaw = 0f;

        [System.NonSerialized] public float lastEdgeZ; // far edge of the most recently spawned object
        [System.NonSerialized] public float gapRoll;   // 0..1, decides the empty space before the next object
        [System.NonSerialized] public float treeRoll;  // 0..1, decides if the next object is a tree
    }

    class Active
    {
        public Transform t;
        public float farEdgeOffset; // far Z edge relative to the pivot
    }

    [Header("Building pools")]
    [Tooltip("City buildings. One pool with all prefabs is fine; with several, one is picked at random.")]
    [FormerlySerializedAs("pools")]
    [SerializeField] ObjectPool[] cityBuildings;
    [Tooltip("Village buildings.")]
    [SerializeField] ObjectPool[] villageBuildings;

    [Header("Tree pools")]
    [Tooltip("Trees. Used in the Village (between buildings) and in the Jungle (only trees).")]
    [SerializeField] ObjectPool[] trees;

    [Tooltip("Village: chance (0-1) that the next object is a tree instead of a building.")]
    [Range(0f, 1f)]
    [SerializeField] float villageTreeChance = 0.35f;

    [Tooltip("Give every tree a random Y rotation, so they don't all look the same.")]
    [SerializeField] bool randomTreeYaw = true;

    [Header("Speed")]
    [Tooltip("Set this to the same value as the road's base speed (units/s). Everything moves toward -Z.")]
    [SerializeField] float baseSpeed = 20f;

    [Header("Road alignment")]
    [Tooltip("Distance from the road center to the road edge. The inner edge of every building is placed here.")]
    [SerializeField] float roadHalfWidth = 8f;

    [Tooltip("Buildings: extra random distance from the road edge (min, max). Keep (0,0) so every building touches the same line.")]
    [SerializeField] Vector2 setback = Vector2.zero;

    [Tooltip("Trees (Village): extra random distance from the road edge (min, max). A bit of spread looks more natural.")]
    [SerializeField] Vector2 treeSetback = new Vector2(0f, 3f);

    [Header("Jungle scatter")]
    [Tooltip("Jungle: extra random distance from the road edge (min, max). Bigger = trees spread deeper into the jungle.")]
    [SerializeField] Vector2 jungleTreeSetback = new Vector2(0f, 25f);

    [Tooltip("Jungle: empty space before a tree along the road (min, max). Bigger = sparser.")]
    [SerializeField] Vector2 jungleTreeGap = new Vector2(1f, 8f);

    [Header("Sides")]
    [SerializeField] Side left = new Side { direction = -1, yaw = 0f };
    [SerializeField] Side right = new Side { direction = 1, yaw = 180f };

    [Header("Spacing")]
    [Tooltip("Empty space before a building along the road (min, max).")]
    [SerializeField] Vector2 gap = new Vector2(0.5f, 3f);

    [Tooltip("Village: empty space before a tree along the road (min, max). Smaller = denser.")]
    [SerializeField] Vector2 treeGap = new Vector2(0f, 2f);

    [Tooltip("Footprint used for an object that has no renderers (width, length).")]
    [SerializeField] Vector2 fallbackSize = new Vector2(10f, 10f);

    [Header("Lifetime")]
    [Tooltip("Objects go back to the pool when they are this far behind the spawner. Spawn distance to the player + a margin.")]
    [SerializeField] float despawnDistance = 100f;

    [Tooltip("Fill the whole road at start so it isn't empty.")]
    [SerializeField] bool prewarm = true;

    public float BaseSpeed { get => baseSpeed; set => baseSpeed = value; }

    readonly List<Active> active = new List<Active>();
    EnvironmentType current = EnvironmentType.City;
    bool initialized;

    void OnEnable()
    {
        EnvironmentManager.Changed += OnEnvironmentChanged;
    }

    void OnDisable()
    {
        EnvironmentManager.Changed -= OnEnvironmentChanged;
    }

    void Start()
    {
        if (EnvironmentManager.Instance != null)
            current = EnvironmentManager.Instance.Current;
        else
            Debug.LogWarning("BuildingSpawner: no EnvironmentManager in the scene, staying on City.", this);

        if (!HasPool(cityBuildings) && !HasPool(villageBuildings) && !HasPool(trees))
        {
            Debug.LogError("BuildingSpawner: assign at least one building or tree Pool.", this);
            enabled = false;
        }
    }

    // New objects use the new environment. Objects already on screen scroll away on their own.
    void OnEnvironmentChanged(EnvironmentType type)
    {
        current = type;

        if (GetTreeChance(type) < 1f && !HasPool(GetBuildingPools(type)))
            Debug.LogWarning($"BuildingSpawner: no building pools assigned for {type}.", this);
        if (GetTreeChance(type) > 0f && !HasPool(trees))
            Debug.LogWarning($"BuildingSpawner: no tree pools assigned, so {type} has no trees.", this);
    }

    // First fill happens in Update (not Start) so the pools are guaranteed to be set up already
    void Initialize()
    {
        initialized = true;

        float startZ = prewarm ? transform.position.z - despawnDistance : transform.position.z;

        left.lastEdgeZ = startZ;
        right.lastEdgeZ = startZ;
        Reroll(left);
        Reroll(right);

        FillSide(left);
        FillSide(right);
    }

    void Update()
    {
        if (!initialized) Initialize();

        if (!GameManager.IsPlaying) return; // no movement in menu, pause or game over

        float delta = baseSpeed * Time.deltaTime;
        float limit = transform.position.z - despawnDistance;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Active a = active[i];

            if (a.t == null) { active.RemoveAt(i); continue; }

            Vector3 p = a.t.position;
            p.z -= delta;
            a.t.position = p;

            // Back to the pool once the whole object (its far edge) is behind the limit
            if (p.z + a.farEdgeOffset < limit)
            {
                ReturnToPool(a.t.gameObject);
                active.RemoveAt(i);
            }
        }

        left.lastEdgeZ -= delta;
        right.lastEdgeZ -= delta;

        FillSide(left);
        FillSide(right);
    }

    void FillSide(Side side)
    {
        if (!side.enabled) return;

        float spawnZ = transform.position.z;

        for (int safety = 0; safety < 64; safety++)
        {
            ObjectPool[] pools = ResolvePools(side, out bool isTree);

            // Nothing can spawn here (e.g. jungle without tree pools): restart the chain at the spawn point,
            // so when objects come back they don't try to fill the area that is already behind us
            if (pools == null)
            {
                side.lastEdgeZ = Mathf.Max(side.lastEdgeZ, spawnZ);
                return;
            }

            bool jungle = current == EnvironmentType.Jungle;

            // Near edge of the next object sits "gap" after the previous object's far edge.
            // This doesn't depend on its size, so we only take one from the pool when it's time.
            Vector2 range = !isTree ? gap : (jungle ? jungleTreeGap : treeGap);
            float nearEdgeZ = side.lastEdgeZ + Mathf.Lerp(range.x, range.y, side.gapRoll);
            if (nearEdgeZ > spawnZ) return;

            ObjectPool pool = PickPool(pools);
            if (pool == null) return;

            float yaw = isTree && randomTreeYaw ? Random.Range(0f, 360f) : side.yaw;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

            GameObject obj = pool.Get(new Vector3(0f, transform.position.y, spawnZ), rot);
            if (obj == null)
            {
                Debug.LogWarning($"BuildingSpawner: pool '{pool.name}' returned nothing. Is its Prefabs list filled in and Can Grow enabled?", pool);
                return; // try again next frame
            }

            obj.transform.rotation = rot; // in case the pool changed it

            // Measure the real size of this object, relative to its pivot
            GetFootprint(obj.transform, out float minX, out float maxX, out float minZ, out float maxZ);

            float pivotZ = nearEdgeZ - minZ;

            // The edge facing the road goes on the road edge line (plus the setback)
            Vector2 sb = !isTree ? setback : (jungle ? jungleTreeSetback : treeSetback);
            float edgeX = side.direction * (roadHalfWidth + Random.Range(sb.x, sb.y));
            float pivotX = side.direction < 0 ? edgeX - maxX : edgeX - minX;

            obj.transform.position = new Vector3(pivotX, transform.position.y, pivotZ);

            active.Add(new Active { t = obj.transform, farEdgeOffset = maxZ });

            side.lastEdgeZ = pivotZ + maxZ;
            Reroll(side);
        }
    }

    static void Reroll(Side side)
    {
        side.gapRoll = Random.value;
        side.treeRoll = Random.value;
    }

    // Decides what the next object on this side is: a tree or a building, based on the current environment.
    // Returns null if nothing can spawn.
    ObjectPool[] ResolvePools(Side side, out bool isTree)
    {
        float chance = GetTreeChance(current);
        ObjectPool[] buildings = GetBuildingPools(current);

        bool canTree = chance > 0f && HasPool(trees);
        bool canBuilding = chance < 1f && HasPool(buildings);

        bool wantTree = chance >= 1f || (chance > 0f && side.treeRoll < chance);

        isTree = wantTree && canTree;
        if (!isTree && !canBuilding && canTree) isTree = true; // e.g. a village with no building pools

        if (isTree) return trees;
        return canBuilding ? buildings : null;
    }

    // Chance that the next object is a tree. City: none, Village: some, Jungle: always.
    float GetTreeChance(EnvironmentType type)
    {
        switch (type)
        {
            case EnvironmentType.Village: return villageTreeChance;
            case EnvironmentType.Jungle: return 1f;
            default: return 0f;
        }
    }

    // To add an environment: add it to EnvironmentType, add an ObjectPool[] field, add a case here
    ObjectPool[] GetBuildingPools(EnvironmentType type)
    {
        switch (type)
        {
            case EnvironmentType.City: return cityBuildings;
            case EnvironmentType.Village: return villageBuildings;
            default: return null; // Jungle: trees only
        }
    }

    static bool HasPool(ObjectPool[] pools)
    {
        if (pools == null) return false;
        foreach (ObjectPool p in pools) if (p != null) return true;
        return false;
    }

    static ObjectPool PickPool(ObjectPool[] pools)
    {
        if (pools == null || pools.Length == 0) return null;

        // Random start so null entries don't get skipped in a biased way
        int start = Random.Range(0, pools.Length);
        for (int i = 0; i < pools.Length; i++)
        {
            ObjectPool p = pools[(start + i) % pools.Length];
            if (p != null) return p;
        }
        return null;
    }

    // Size of the object relative to its pivot, in world axes (after rotation)
    void GetFootprint(Transform t, out float minX, out float maxX, out float minZ, out float maxZ)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            maxX = fallbackSize.x * 0.5f; minX = -maxX;
            maxZ = fallbackSize.y * 0.5f; minZ = -maxZ;
            return;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        Vector3 pivot = t.position;
        minX = b.min.x - pivot.x; maxX = b.max.x - pivot.x;
        minZ = b.min.z - pivot.z; maxZ = b.max.z - pivot.z;
    }

    void ReturnToPool(GameObject obj)
    {
        if (obj.TryGetComponent(out PooledObject pooled) && pooled.Pool != null)
            pooled.Pool.Return(obj);
        else
            obj.SetActive(false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 o = transform.position;

        // Road edge lines where the buildings will stick
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-roadHalfWidth, o.y, o.z), new Vector3(-roadHalfWidth, o.y, o.z - despawnDistance));
        Gizmos.DrawLine(new Vector3(roadHalfWidth, o.y, o.z), new Vector3(roadHalfWidth, o.y, o.z - despawnDistance));

        // Despawn line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-roadHalfWidth - 15f, o.y, o.z - despawnDistance),
                        new Vector3(roadHalfWidth + 15f, o.y, o.z - despawnDistance));
    }
#endif
}