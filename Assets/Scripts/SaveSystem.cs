using UnityEngine;

public static class SaveSystem
{
    const string HighScoreKey = "HighScore";
    const string MoneyKey = "Money";

    public static int Money
    {
        get => PlayerPrefs.GetInt(MoneyKey, 0);
        set => PlayerPrefs.SetInt(MoneyKey, value);
    }

    public static int HighScore
    {
        get => PlayerPrefs.GetInt(HighScoreKey, 0);
        set => PlayerPrefs.SetInt(HighScoreKey, value);
    }

    public static void Save() => PlayerPrefs.Save();
}