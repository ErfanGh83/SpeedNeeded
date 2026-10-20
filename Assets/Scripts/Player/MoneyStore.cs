using System;
using UnityEngine;

public static class MoneyStore
{
    static int balance;
    static bool loaded;

    public static int Balance
    {
        get { EnsureLoaded(); return balance; }
    }

    public static int RunEarned { get; private set; }

    public static event Action<int, int> OnMoneyChanged;

    static void EnsureLoaded()
    {
        if (loaded) return;
        balance = SaveSystem.Money;
        loaded = true;
    }

    public static void BeginRun()
    {
        EnsureLoaded();
        RunEarned = 0;
        OnMoneyChanged?.Invoke(balance, RunEarned);
    }

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        EnsureLoaded();

        balance += amount;
        RunEarned += amount;
        SaveSystem.Money = balance; // in memory now, written to disk on game over / pause / quit

        Debug.Log($"[Money] +{amount} | balance {balance} | this run {RunEarned}");
        OnMoneyChanged?.Invoke(balance, RunEarned);
    }

    public static bool CanAfford(int cost)
    {
        EnsureLoaded();
        return cost >= 0 && balance >= cost;
    }

    public static bool TrySpend(int cost)
    {
        EnsureLoaded();

        if (cost <= 0 || balance < cost)
        {
            Debug.Log($"[Money] Can't spend {cost}, balance is {balance}");
            return false;
        }

        balance -= cost;
        SaveSystem.Money = balance;
        SaveSystem.Save(); // a purchase should never be lost

        Debug.Log($"[Money] -{cost} | balance {balance}");
        OnMoneyChanged?.Invoke(balance, RunEarned);
        return true;
    }
}