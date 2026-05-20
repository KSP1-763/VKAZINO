using UnityEngine;

public static class BalanceManager
{
    private const string KEY = "Money";

    public static double GetBalance()
    {
        string val = PlayerPrefs.GetString(KEY, "");
        if (double.TryParse(val, out double result)) return result;
        if (PlayerPrefs.HasKey(KEY)) return PlayerPrefs.GetInt(KEY, 500);
        return 500;
    }

    public static void SetBalance(double amount)
    {
        PlayerPrefs.SetString(KEY, amount.ToString());
        PlayerPrefs.Save();
    }

    public static void AddMoney(double amount) => SetBalance(GetBalance() + amount);
    public static bool SpendMoney(double amount)
    {
        if (GetBalance() >= amount)
        {
            SetBalance(GetBalance() - amount);
            return true;
        }
        return false;
    }
}