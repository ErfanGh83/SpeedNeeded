using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Tooltip("The prefabs this pool can spawn. Get() picks one at random.")]
    [SerializeField] GameObject[] prefabs;

    [Tooltip("Instances created up front for EACH prefab.")]
    [SerializeField] int prewarmPerPrefab = 4;

    [Tooltip("Create extra instances when a variant runs out. Turn off to cap the number of active objects.")]
    [SerializeField] bool canGrow = true;

    Queue<GameObject>[] available;

    void Awake()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError($"ObjectPool '{name}': the Prefabs list is empty.", this);
            prefabs = new GameObject[0];
        }

        available = new Queue<GameObject>[prefabs.Length];

        for (int i = 0; i < prefabs.Length; i++)
        {
            available[i] = new Queue<GameObject>();

            if (prefabs[i] == null)
            {
                Debug.LogError($"ObjectPool '{name}': Prefabs element {i} is empty.", this);
                continue;
            }

            for (int j = 0; j < prewarmPerPrefab; j++)
                available[i].Enqueue(CreateInstance(i));
        }
    }

    GameObject CreateInstance(int prefabIndex)
    {
        GameObject obj = Instantiate(prefabs[prefabIndex], transform);
        obj.SetActive(false);

        PooledObject pooled = obj.GetComponent<PooledObject>();
        if (pooled == null) pooled = obj.AddComponent<PooledObject>();
        pooled.Pool = this;
        pooled.PrefabIndex = prefabIndex;

        return obj;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        if (prefabs == null || prefabs.Length == 0) return null;

        int index = Random.Range(0, prefabs.Length);
        Queue<GameObject> queue = available[index];

        GameObject obj;
        if (queue.Count > 0) obj = queue.Dequeue();
        else if (canGrow) obj = CreateInstance(index);
        else return null;

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null || !obj.activeSelf) return; // already returned: avoids duplicates in the queue

        obj.SetActive(false);

        PooledObject pooled = obj.GetComponent<PooledObject>();
        if (pooled != null) available[pooled.PrefabIndex].Enqueue(obj);
    }
}
