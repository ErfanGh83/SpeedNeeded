using UnityEngine;

public class HealthPickup : Interactable
{
    [SerializeField] int amount = 1;

    protected override void OnInteract(CarController player)
    {
        HealthSystem health = player.GetComponent<HealthSystem>();
        if (health != null) health.Heal(amount);

        Despawn();
    }
}