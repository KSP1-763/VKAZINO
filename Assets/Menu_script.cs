using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Menu_script : MonoBehaviour
{
    public TextMeshProUGUI balanceText;

    void OnEnable() => UpdateBalance();
    void Start() => UpdateBalance();

    public void UpdateBalance()
    {
        if (balanceText != null)
            balanceText.text = "Ваш баланс: " + BalanceManager.GetBalance().ToString("F0");
    }

    public void OpenPoker() => SceneManager.LoadScene("Poker");
    public void OpenDurak() => SceneManager.LoadScene("Durak");
    public void OpenGuessNumber() => SceneManager.LoadScene("GuessNumber");
    public void OpenPromo() => SceneManager.LoadScene("Promo");
    public void OpenBlackjack() => SceneManager.LoadScene("Blackjack");
    public void OpenSlots() => SceneManager.LoadScene("Slots");
    public void OpenSettings() => SceneManager.LoadScene("Settings");
    public void ExitGame() => Application.Quit();
}