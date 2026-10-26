using System.Collections.Generic;
using UnityEngine;

public class RoadLightSpawner : MonoBehaviour
{
    [System.Serializable]
    class Side
    {
        public bool enabled = true;
        [Tooltip("-1 = left of the road, +1 = right of the road.")]
        public int direction = -1;
        [Tooltip("Y rotation of the lights on this side. Use 180 on the right so they face the road.")]
        public float yaw = 0f;
        [Tooltip("Shifts this side's lights along the road, as a fraction of the spacing (0.5 = halfway between the other side's lights).")]
        [Range(0f, 1f)] public float offsetFraction = 0f;

        [System.NonSerialized] public float lastZ;
    }

    class Active
    {
        public Transform t;
    }

    [Header("Pool")]
    [SerializeField] ObjectPool pool;

    [Header("Speed")]
    [Tooltip("Constant scroll speed (world units/s) added on top of the game speed. Use the same value as the road's ScrollTexture.")]
    [SerializeField] float baseScrollSpeed = 5f;

    [Tooltip("Multiplies the game-speed part. Use the same value as the road's ScrollTexture.")]
    [SerializeField] float speedMultiplier = 1f;

    [Header("Placement")]
    [Tooltip("Distance from the road center to the road edge. Use the same value as the BuildingSpawner.")]
    [SerializeField] float roadHalfWidth = 8f;

    [Tooltip("Extra distance from the road edge to the light's pivot.")]
    [SerializeField] float sideOffset = 0.5f;

    [Tooltip("Distance between two lights on the same side (world units).")]
    [SerializeField] float spacing = 20f;

    [SerializeField] Side left = new Side { direction = -1, yaw = 0f };
    [SerializeField] Side right = new Side { direction = 1, yaw = 180f };

    [Header("Lifetime")]
    [Tooltip("Lights go back to the pool when they are this far behind the spawner. Spawn distance to the player + a margin.")]
    [SerializeField] float despawnDistance = 100f;

    [Tooltip("Fill the whole road with lights at start so it isn't empty.")]
    [SerializeField] bool prewarm = true;

    readonly List<Active> active = new List<Active>();
    bool initialized;

    void Start()
    {
        if (pool == null)
        {
            Debug.LogError("RoadLightSpawner: assign an Object Pool.", this);
            enabled = false;
            return;
        }

        if (spacing <= 0.1f)
        {
            Debug.LogError("RoadLightSpawner: spacing must be greater than 0.", this);
            enabled = false;
        }
    }

    void Initialize()
    {
        initialized = true;

        float startZ = prewarm ? transform.position.z - despawnDistance : transform.position.z;

        left.lastZ = startZ - spacing + left.offsetFraction * spacing;
        right.lastZ = startZ - spacing + right.offsetFraction * spacing;

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

            if (p.z < limit)
            {
                ReturnToPool(a.t.gameObject);
                active.RemoveAt(i);
            }
        }

        left.lastZ -= delta;
        right.lastZ -= delta;

        FillSide(left);
        FillSide(right);
    }

    void FillSide(Side side)
    {
        if (!side.enabled) return;

        float spawnZ = transform.position.z;

        for (int safety = 0; safety < 64; safety++)
        {
            float z = side.lastZ + spacing;
            if (z > spawnZ) return;

            float x = side.direction * (roadHalfWidth + sideOffset);
            Quaternion rot = Quaternion.Euler(0f, side.yaw, 0f);

            GameObject obj = pool.Get(new Vector3(x, transform.position.y, z), rot);
            if (obj == null)
            {
                Debug.LogWarning($"RoadLightSpawner: pool '{pool.name}' returned nothing. Is its Prefabs list filled in and Can Grow enabled?", pool);
                return;
            }

            obj.transform.SetPositionAndRotation(new Vector3(x, transform.position.y, z), rot);

            active.Add(new Active { t = obj.transform });
            side.lastZ = z;
        }
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
        float x = roadHalfWidth + sideOffset;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(-x, o.y, o.z), new Vector3(-x, o.y, o.z - despawnDistance));
        Gizmos.DrawLine(new Vector3(x, o.y, o.z), new Vector3(x, o.y, o.z - despawnDistance));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-x - 5f, o.y, o.z - despawnDistance), new Vector3(x + 5f, o.y, o.z - despawnDistance));
    }
#endif
}