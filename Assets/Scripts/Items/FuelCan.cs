using UnityEngine;

public class FuelCan : Interactable
{
    [SerializeField] float amount = 25f;

    protected override void OnInteract(CarController player)
    {
        FuelSystem fuel = player.GetComponent<FuelSystem>();
        if (fuel != null)
        {
            fuel.Add(amount);
            Debug.Log($"[FuelCan] +{amount} fuel");
        }

        Despawn();
    }
}