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
    public Sprite questionSprite;
    public int defaultBet = 10;
    public int[] winMultipliers = { 0, 2, 5, 10, 20, 50 };

    private double balance;
    private bool isSpinning = false;
    private int[] currentSymbols = new int[3];

    void Start()
    {
        spinButton.onClick.AddListener(Spin);
        // Подписываемся на событие изменения баланса
        BalanceManager.OnBalanceChanged += OnBalanceChanged;
        LoadBalance();
        UpdateBalanceUI();
        foreach (var reel in reels)
            if (questionSprite != null) reel.sprite = questionSprite;
    }

    void OnDestroy()
    {
        BalanceManager.OnBalanceChanged -= OnBalanceChanged;
    }

    void OnEnable()
    {
        LoadBalance();
        UpdateBalanceUI();
    }

    void OnBalanceChanged()
    {
        LoadBalance();
        UpdateBalanceUI();
    }

    void LoadBalance() => balance = BalanceManager.GetBalance();
    void SaveBalance() => BalanceManager.SetBalance(balance);

    void UpdateBalanceUI()
    {
        if (balanceText != null)
            balanceText.text = "Ваш баланс: " + balance.ToString("F0");
    }

    void Spin()
    {
        if (isSpinning) return;
        int bet = defaultBet;
        if (betInput != null && int.TryParse(betInput.text, out int b) && b > 0) bet = b;
        if (balance < bet)
        {
            resultText.text = "Недостаточно денег!";
            return;
        }
        balance -= bet;
        SaveBalance();
        UpdateBalanceUI();
        StartCoroutine(SpinReels(bet));
    }

    IEnumerator SpinReels(int bet)
    {
        isSpinning = true;
        spinButton.interactable = false;
        resultText.text = "Крутим...";
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
        isSpinning = false;
        spinButton.interactable = true;
    }

    void CheckWin(int bet)
    {
        double win = 0;
        string type = "";
        if (currentSymbols[0] == currentSymbols[1] && currentSymbols[1] == currentSymbols[2])
        {
            win = bet * winMultipliers[5];
            type = "JACKPOT!";
        }
        else if (currentSymbols[0] == currentSymbols[1] ||
                 currentSymbols[1] == currentSymbols[2] ||
                 currentSymbols[0] == currentSymbols[2])
        {
            win = bet * winMultipliers[2];
            type = "ДВА ОДИНАКОВЫХ!";
        }

        if (win > 0)
        {
            balance += win;
            SaveBalance();
            UpdateBalanceUI();
            resultText.text = type + " Выигрыш: " + win.ToString("F0") + " монет!";
        }
        else
        {
            resultText.text = "Повезёт в следующий раз!";
        }

        BalanceManager.OnBalanceChanged?.Invoke();
    }

    public void BackToMenu()
    {
        BalanceManager.OnBalanceChanged?.Invoke();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}