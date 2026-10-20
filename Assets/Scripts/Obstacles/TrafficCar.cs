using UnityEngine;

public class TrafficCar : Interactable
{
    protected override void OnInteract(CarController player)
    {
        HealthSystem health = player.GetComponent<HealthSystem>();

        if (health != null) health.TakeDamage(1);
        else if (GameManager.Instance != null) GameManager.Instance.GameOver("Crashed into a car");
    }
}