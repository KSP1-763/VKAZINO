using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Promo : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text moneyPromo, moneyMenu, messageText;
    public double minReward = 1000;   // минимальная награда
    public double maxReward = 50000;  // максимальная награда
    public string[] promoCodes;

    void OnEnable() => UpdateUI();

    public void Confirm()
    {
        string code = inputField.text;
        foreach (string c in promoCodes)
        {
            if (code == c)
            {
                string key = "Used_" + code;
                if (PlayerPrefs.GetInt(key, 0) == 1)
                {
                    ShowMessage("Промокод уже использован", Color.red);
                    return;
                }

                // Случайная награда от minReward до maxReward
                double reward = Random.Range((float)minReward, (float)maxReward);
                BalanceManager.AddMoney(reward);

                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                UpdateUI();
                ShowMessage($"Промокод активирован! +{reward:F0} монет", Color.green);
                inputField.text = "";
                return;
            }
        }
        ShowMessage("Неверный промокод", Color.red);
    }

    void ShowMessage(string text, Color color)
    {
        messageText.text = text;
        messageText.color = color;
        CancelInvoke("ClearMsg");
        Invoke("ClearMsg", 2f);
    }

    void ClearMsg() => messageText.text = "";

    void UpdateUI()
    {
        double bal = BalanceManager.GetBalance();
        if (moneyPromo) moneyPromo.text = "Баланс: " + bal.ToString("F0");
        if (moneyMenu) moneyMenu.text = "Баланс: " + bal.ToString("F0");
    }

    public void ResetPromo()
    {
        BalanceManager.SetBalance(500);
        foreach (string c in promoCodes)
            PlayerPrefs.DeleteKey("Used_" + c);
        PlayerPrefs.Save();
        UpdateUI();
        ShowMessage("Промокоды сброшены! Баланс: 500", Color.green);
    }

    public void BackToMenu() => SceneManager.LoadScene("MainMenu");
}