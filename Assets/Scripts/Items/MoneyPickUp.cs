using UnityEngine;

public class MoneyPickup : Interactable
{
    [Tooltip("Money added to the wallet.")]
    [SerializeField] int amount = 10;

    [Tooltip("Extra score points for picking it up (0 = none).")]
    [SerializeField] int scoreBonus = 10;

    protected override void OnInteract(CarController player)
    {
        MoneyStore.Add(amount);

        if (scoreBonus > 0 && GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreBonus);

        Despawn();
    }
}