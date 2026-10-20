using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public ObjectPool Pool { get; set; }
    public int PrefabIndex { get; set; }

    public void ReturnToPool()
    {
        if (Pool != null) Pool.Return(gameObject);
        else gameObject.SetActive(false);
    }
}
