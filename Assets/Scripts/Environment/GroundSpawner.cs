using System.Collections.Generic;
using UnityEngine;

public class GroundSpawner : MonoBehaviour
{
    [System.Serializable]
    class Side
    {
        public bool enabled = true;
        [Tooltip("-1 = left of the road, +1 = right of the road.")]
        public int direction = -1;
        [Tooltip("Y rotation of the tiles on this side. Use 180 if the tile texture or shape should face the other way.")]
        public float yaw = 0f;

        [System.NonSerialized] public float lastEdgeZ;
    }

    class Active
    {
        public Transform t;
        public float farEdgeOffset;
    }

    [Header("Ground pools")]
    [Tooltip("Used in the City environment.")]
    [SerializeField] ObjectPool[] cityTiles;
    [Tooltip("Used in the Village and Jungle environments.")]
    [SerializeField] ObjectPool[] grassTiles;

    [Header("Speed")]
    [Tooltip("Constant scroll speed (world units/s) added on top of the game speed. Use the same value as the road's ScrollTexture.")]
    [SerializeField] float baseScrollSpeed = 5f;

    [Tooltip("Multiplies the game-speed part. Use the same value as the road's ScrollTexture.")]
    [SerializeField] float speedMultiplier = 1f;

    [Header("Road alignment")]
    [Tooltip("Distance from the road center to the road edge. Use the same value as the BuildingSpawner.")]
    [SerializeField] float roadHalfWidth = 8f;

    [Tooltip("Pushes the tiles' inner edge this far under the road, so no gap shows between road and ground.")]
    [SerializeField] float edgeOverlap = 0.05f;

    [Header("Sides")]
    [SerializeField] Side left = new Side { direction = -1, yaw = 0f };
    [SerializeField] Side right = new Side { direction = 1, yaw = 0f };

    [Header("Tiles")]
    [Tooltip("Tiles overlap by this much along Z so no thin seams show between them. Keep it small.")]
    [SerializeField] float seamOverlap = 0.01f;

    [Tooltip("Footprint used for a tile that has no renderers (width, length).")]
    [SerializeField] Vector2 fallbackSize = new Vector2(20f, 20f);

    [Header("Lifetime")]
    [Tooltip("Tiles go back to the pool when they are this far behind the spawner. Spawn distance to the player + a margin.")]
    [SerializeField] float despawnDistance = 100f;

    [Tooltip("Fill the whole road with ground at start so it isn't empty.")]
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
            Debug.LogWarning("GroundSpawner: no EnvironmentManager in the scene, staying on City.", this);

        if (!HasPool(GetPools(current)))
        {
            Debug.LogError($"GroundSpawner: assign at least one Pool for the start environment ({current}).", this);
            enabled = false;
        }
    }

    void OnEnvironmentChanged(EnvironmentType type)
    {
        if (!HasPool(GetPools(type)))
        {
            Debug.LogWarning($"GroundSpawner: no ground Pool assigned for {type}, keeping {current}.", this);
            return;
        }

        current = type;
    }

    void Initialize()
    {
        initialized = true;

        float startZ = prewarm ? transform.position.z - despawnDistance : transform.position.z;
        left.lastEdgeZ = startZ;
        right.lastEdgeZ = startZ;

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
        ObjectPool[] pools = GetPools(current);

        for (int safety = 0; safety < 64; safety++)
        {
            float nearEdgeZ = side.lastEdgeZ - seamOverlap;
            if (nearEdgeZ > spawnZ) return;

            ObjectPool pool = PickPool(pools);
            if (pool == null) return;

            Quaternion rot = Quaternion.Euler(0f, side.yaw, 0f);
            GameObject obj = pool.Get(new Vector3(0f, transform.position.y, spawnZ), rot);
            if (obj == null)
            {
                Debug.LogWarning($"GroundSpawner: pool '{pool.name}' returned nothing. Is its Prefabs list filled in and Can Grow enabled?", pool);
                return;
            }

            obj.transform.rotation = rot;

            GetFootprint(obj.transform, out float minX, out float maxX, out float minZ, out float maxZ);

            float pivotZ = nearEdgeZ - minZ;

            float edgeX = side.direction * (roadHalfWidth - edgeOverlap);
            float pivotX = side.direction < 0 ? edgeX - maxX : edgeX - minX;

            obj.transform.position = new Vector3(pivotX, transform.position.y, pivotZ);

            active.Add(new Active { t = obj.transform, farEdgeOffset = maxZ });
            side.lastEdgeZ = pivotZ + maxZ;
        }
    }

    ObjectPool[] GetPools(EnvironmentType type)
    {
        switch (type)
        {
            case EnvironmentType.City: return cityTiles;
            case EnvironmentType.Village:
            case EnvironmentType.Jungle: return grassTiles;
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

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-roadHalfWidth, o.y, o.z), new Vector3(-roadHalfWidth, o.y, o.z - despawnDistance));
        Gizmos.DrawLine(new Vector3(roadHalfWidth, o.y, o.z), new Vector3(roadHalfWidth, o.y, o.z - despawnDistance));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-roadHalfWidth - 15f, o.y, o.z - despawnDistance),
                        new Vector3(roadHalfWidth + 15f, o.y, o.z - despawnDistance));
    }
#endif
}