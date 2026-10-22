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

        [System.NonSerialized] public float lastEdgeZ;
        [System.NonSerialized] public float gapRoll;
        [System.NonSerialized] public float treeRoll;
    }

    class Active
    {
        public Transform t;
        public float farEdgeOffset;
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
    [Tooltip("Constant scroll speed (world units/s) added on top of the game speed. Use the same value as the road's ScrollTexture.")]
    [SerializeField] float baseScrollSpeed = 5f;

    [Tooltip("Multiplies the game-speed part. Use the same value as the road's ScrollTexture.")]
    [SerializeField] float speedMultiplier = 1f;

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

    void OnEnvironmentChanged(EnvironmentType type)
    {
        current = type;

        if (GetTreeChance(type) < 1f && !HasPool(GetBuildingPools(type)))
            Debug.LogWarning($"BuildingSpawner: no building pools assigned for {type}.", this);
        if (GetTreeChance(type) > 0f && !HasPool(trees))
            Debug.LogWarning($"BuildingSpawner: no tree pools assigned, so {type} has no trees.", this);
    }

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

        if (!GameManager.IsPlaying) return;

        float delta = (baseScrollSpeed + WorldSpeed.Value * speedMultiplier) * Time.deltaTime;
        float limit = transform.position.z - despawnDistance;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Active a = active[i];

            if (a.t == null) { active.RemoveAt(i); continue; }

            Vector3 p = a.t.position;
            p.z -= delta;
            a.t.position = p;

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

            if (pools == null)
            {
                side.lastEdgeZ = Mathf.Max(side.lastEdgeZ, spawnZ);
                return;
            }

            bool jungle = current == EnvironmentType.Jungle;

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
                return;
            }

            obj.transform.rotation = rot;

            GetFootprint(obj.transform, out float minX, out float maxX, out float minZ, out float maxZ);

            float pivotZ = nearEdgeZ - minZ;

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

    ObjectPool[] ResolvePools(Side side, out bool isTree)
    {
        float chance = GetTreeChance(current);
        ObjectPool[] buildings = GetBuildingPools(current);

        bool canTree = chance > 0f && HasPool(trees);
        bool canBuilding = chance < 1f && HasPool(buildings);

        bool wantTree = chance >= 1f || (chance > 0f && side.treeRoll < chance);

        isTree = wantTree && canTree;
        if (!isTree && !canBuilding && canTree) isTree = true;

        if (isTree) return trees;
        return canBuilding ? buildings : null;
    }

    float GetTreeChance(EnvironmentType type)
    {
        switch (type)
        {
            case EnvironmentType.Village: return villageTreeChance;
            case EnvironmentType.Jungle: return 1f;
            default: return 0f;
        }
    }

    ObjectPool[] GetBuildingPools(EnvironmentType type)
    {
        switch (type)
        {
            case EnvironmentType.City: return cityBuildings;
            case EnvironmentType.Village: return villageBuildings;
            default: return null;
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

        int start = Random.Range(0, pools.Length);
        for (int i = 0; i < pools.Length; i++)
        {
            ObjectPool p = pools[(start + i) % pools.Length];
            if (p != null) return p;
        }
        return null;
    }

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

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-roadHalfWidth, o.y, o.z), new Vector3(-roadHalfWidth, o.y, o.z - despawnDistance));
        Gizmos.DrawLine(new Vector3(roadHalfWidth, o.y, o.z), new Vector3(roadHalfWidth, o.y, o.z - despawnDistance));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-roadHalfWidth - 15f, o.y, o.z - despawnDistance),
                        new Vector3(roadHalfWidth + 15f, o.y, o.z - despawnDistance));
    }
#endif
}