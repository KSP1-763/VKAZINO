using UnityEngine;

public static class BalanceManager
{
    private const string MONEY_KEY = "Money";
    public static System.Action OnBalanceChanged;

    public static double GetBalance()
    {
        string data = PlayerPrefs.GetString(MONEY_KEY, "");
        if (double.TryParse(data, out double bal)) return bal;
        if (PlayerPrefs.HasKey(MONEY_KEY))
            return PlayerPrefs.GetInt(MONEY_KEY, 500);
        return 500;
    }

    public static void SetBalance(double amount)
    {
        PlayerPrefs.SetString(MONEY_KEY, amount.ToString());
        PlayerPrefs.Save();
        OnBalanceChanged?.Invoke();
    }

    public static void AddMoney(double amount)
    {
        double current = GetBalance();
        SetBalance(current + amount);
    }

    public static bool SpendMoney(double amount)
    {
        double current = GetBalance();
        if (current >= amount)
        {
            SetBalance(current - amount);
            return true;
        }
        return false;
    }
}