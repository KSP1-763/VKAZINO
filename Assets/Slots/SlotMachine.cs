using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotMachine : MonoBehaviour
{
    public TMP_InputField betInput;
    public Button spinButton;
    public TextMeshProUGUI balanceText, resultText;
    public Image[] reels;
    public Sprite[] symbols;
    public int defaultBet = 10;

    private double balance;
    private bool spinning = false;
    private int[] currentSymbols = new int[3];

    void Start()
    {
        spinButton.onClick.AddListener(Spin);
        LoadBalance();
        UpdateUI();
    }

    void LoadBalance() => balance = BalanceManager.GetBalance();
    void SaveBalance() => BalanceManager.SetBalance(balance);
    void UpdateUI() => balanceText.text = "Ваш баланс: " + balance.ToString("F0");

    void Spin()
    {
        if (spinning) return;
        int bet = defaultBet;
        if (int.TryParse(betInput.text, out int b) && b > 0) bet = b;
        if (balance < bet)
        {
            resultText.text = "Не хватает денег";
            return;
        }
        balance -= bet;
        SaveBalance();
        UpdateUI();
        StartCoroutine(SpinAnimation(bet));
    }

    IEnumerator SpinAnimation(int bet)
    {
        spinning = true;
        spinButton.interactable = false;
        for (int t = 0; t < 20; t++)
        {
            for (int i = 0; i < reels.Length; i++)
            {
                currentSymbols[i] = Random.Range(0, symbols.Length);
                reels[i].sprite = symbols[currentSymbols[i]];
            }
            yield return new WaitForSeconds(0.05f);
        }
        for (int i = 0; i < reels.Length; i++)
        {
            currentSymbols[i] = Random.Range(0, symbols.Length);
            reels[i].sprite = symbols[currentSymbols[i]];
        }
        CheckWin(bet);
        spinning = false;
        spinButton.interactable = true;
    }

    void CheckWin(int bet)
    {
        double win = 0;
        if (currentSymbols[0] == currentSymbols[1] && currentSymbols[1] == currentSymbols[2])
            win = bet * 50;
        else if (currentSymbols[0] == currentSymbols[1] ||
                 currentSymbols[1] == currentSymbols[2] ||
                 currentSymbols[0] == currentSymbols[2])
            win = bet * 5;

        if (win > 0)
        {
            balance += win;
            SaveBalance();
            UpdateUI();
            resultText.text = $"Выигрыш: {win:F0} монет!";
        }
        else resultText.text = "Повезёт в следующий раз!";
    }

    public void BackToMenu() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
}