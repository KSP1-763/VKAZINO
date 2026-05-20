using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuessNumber : MonoBehaviour
{
    public TMP_InputField minInput, maxInput, guessInput, betInput;
    public Button guessButton, newGameButton, backButton;
    public TextMeshProUGUI messageText, attemptsText, balanceText, betAmountText;
    public int defaultBet = 50;
    public float lossPercent = 0.1f;
    public int winMultiplier = 5;

    private int minNumber, maxNumber, targetNumber, attempts;
    private bool gameActive;
    private double currentBet;

    void Start()
    {
        guessButton.onClick.AddListener(CheckGuess);
        newGameButton.onClick.AddListener(StartNewGame);
        backButton.onClick.AddListener(BackToMenu);
        UpdateBalanceUI();
    }

    void OnEnable() => UpdateBalanceUI();

    void UpdateBalanceUI()
    {
        balanceText.text = "Баланс: " + BalanceManager.GetBalance().ToString("F0");
    }

    void UpdateBetDisplay()
    {
        if (betAmountText != null)
            betAmountText.text = $"Ставка: {currentBet:F0}\nШтраф: {currentBet * lossPercent:F0}";
    }

    public void StartNewGame()
    {
        if (!int.TryParse(minInput.text, out minNumber) || !int.TryParse(maxInput.text, out maxNumber))
        {
            messageText.text = "Введите диапазон";
            return;
        }
        if (minNumber >= maxNumber)
        {
            messageText.text = "Минимум < максимума";
            return;
        }

        currentBet = defaultBet;
        if (betInput != null && double.TryParse(betInput.text, out double b) && b > 0)
            currentBet = b;

        if (BalanceManager.GetBalance() < currentBet)
        {
            messageText.text = "Не хватает денег на ставку!";
            return;
        }

        BalanceManager.SpendMoney(currentBet);
        UpdateBalanceUI();
        UpdateBetDisplay();

        targetNumber = Random.Range(minNumber, maxNumber + 1);
        attempts = 0;
        gameActive = true;
        guessInput.interactable = true;
        guessButton.interactable = true;
        guessInput.text = "";
        messageText.text = $"Угадай число от {minNumber} до {maxNumber}. Ставка: {currentBet:F0}";
        UpdateAttemptsUI();
    }

    void CheckGuess()
    {
        if (!gameActive)
        {
            messageText.text = "Нажмите «Новая игра»";
            return;
        }
        if (!int.TryParse(guessInput.text, out int playerGuess))
        {
            messageText.text = "Введите число";
            return;
        }
        if (playerGuess < minNumber || playerGuess > maxNumber)
        {
            messageText.text = $"Число от {minNumber} до {maxNumber}";
            return;
        }

        attempts++;
        UpdateAttemptsUI();

        if (playerGuess == targetNumber)
        {
            double win = currentBet * winMultiplier;
            BalanceManager.AddMoney(currentBet + win);
            UpdateBalanceUI();
            messageText.text = $"ПОБЕДА! Число {targetNumber}. Попыток: {attempts}. Выигрыш +{currentBet + win:F0}";
            gameActive = false;
            guessInput.interactable = false;
            guessButton.interactable = false;
            return;
        }

        double loss = currentBet * lossPercent;
        BalanceManager.SpendMoney(loss);
        UpdateBalanceUI();

        string hint = playerGuess < targetNumber ? "БОЛЬШЕ" : "МЕНЬШЕ";
        messageText.text = $"Загаданное число {hint} {playerGuess}. Штраф: -{loss:F0}";

        if (BalanceManager.GetBalance() <= 0)
        {
            messageText.text = "Деньги кончились! Игра окончена.";
            gameActive = false;
            guessButton.interactable = false;
        }

        guessInput.text = "";
        guessInput.Select();
    }

    void UpdateAttemptsUI()
    {
        if (attemptsText != null)
            attemptsText.text = "Попыток: " + attempts;
    }

    void BackToMenu() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
}