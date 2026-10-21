using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    public enum Source { Balance, EarnedThisRun }

    [Header("References")]
    [SerializeField] TMP_Text moneyText;

    [Header("Display")]
    [SerializeField] Source show = Source.Balance;
    [Tooltip("Optional text before the number, e.g. \"$\". Leave empty for only the number.")]
    [SerializeField] string prefix = "";
    [SerializeField] string suffix = "";
    [SerializeField] bool thousandsSeparator = false;

    void OnEnable()
    {
        MoneyStore.OnMoneyChanged += OnMoneyChanged;
        OnMoneyChanged(MoneyStore.Balance, MoneyStore.RunEarned);
    }

    void OnDisable()
    {
        MoneyStore.OnMoneyChanged -= OnMoneyChanged;
    }

    void OnMoneyChanged(int balance, int runEarned)
    {
        if (moneyText == null) return;

        int value = show == Source.Balance ? balance : runEarned;
        string number = thousandsSeparator ? value.ToString("N0") : value.ToString();
        moneyText.text = prefix + number + suffix;
    }
}