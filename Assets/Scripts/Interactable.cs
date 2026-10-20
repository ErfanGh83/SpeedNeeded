using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    float ownSpeed;
    float despawnDistance;
    float distanceTraveled;
    bool initialized;
    bool consumed;
    PooledObject pooled;

    public void Initialize(float speed, float maxTravelDistance)
    {
        ownSpeed = speed;
        despawnDistance = maxTravelDistance;
        distanceTraveled = 0f;
        consumed = false;
        initialized = true;
    }

    protected virtual void Update()
    {
        // Not spawned by a spawner, or the game is in menu/paused/game over: stay put
        if (!initialized || !GameManager.IsPlaying) return;

        // Positive = approaching the player (-Z), negative = driving away faster than the road
        float step = (WorldSpeed.Value - ownSpeed) * Time.deltaTime;
        transform.position += Vector3.back * step;

        distanceTraveled += Mathf.Abs(step);
        if (distanceTraveled >= despawnDistance) Despawn();
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed || !GameManager.IsPlaying) return;

        CarController player = other.GetComponentInParent<CarController>();
        if (player == null) return;

        consumed = true; // a car with several colliders can't react twice
        Debug.Log($"[Interactable] Player touched {name} ({GetType().Name})", this);
        OnInteract(player);
    }

    protected abstract void OnInteract(CarController player);

    protected void Despawn()
    {
        if (pooled == null) pooled = GetComponent<PooledObject>();

        if (pooled != null) pooled.ReturnToPool();
        else gameObject.SetActive(false);
    }
}