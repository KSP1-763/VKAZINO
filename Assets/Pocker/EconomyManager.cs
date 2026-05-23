using UnityEngine;

public static class EconomyManager
{
    private const string MONEY_KEY = "Money";

    public static int GetMoney()
    {
        return PlayerPrefs.GetInt(MONEY_KEY, 500);
    }

    public static void AddMoney(int amount)
    {
        int current = GetMoney();
        PlayerPrefs.SetInt(MONEY_KEY, current + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendMoney(int amount)
    {
        int current = GetMoney();
        if (current >= amount)
        {
            PlayerPrefs.SetInt(MONEY_KEY, current - amount);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public static void SetMoney(int amount)
    {
        PlayerPrefs.SetInt(MONEY_KEY, amount);
        PlayerPrefs.Save();
    }
}