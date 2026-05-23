using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlackjackGame : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI gameStatusText, playerScoreText, dealerScoreText, balanceText;
    public TMP_InputField betInput;
    public Button startButton, hitButton, standButton, doubleButton;

    [Header("Родители карт")]
    public Transform playerHandParent, dealerHandParent;

    [Header("Префабы")]
    public GameObject cardPrefab;
    public Sprite cardBackSprite;

    private List<Player_card> deck = new List<Player_card>();
    private List<Player_card> playerHand = new List<Player_card>();
    private List<Player_card> dealerHand = new List<Player_card>();
    private List<CardVisual> allCardVisuals = new List<CardVisual>();

    private int playerScore, dealerScore;
    private double betAmount = 50, originalBet;
    private bool gameActive = false, playerTurn = true, hasDoubled = false, isNewGame = true;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        hitButton.onClick.AddListener(Hit);
        standButton.onClick.AddListener(Stand);
        doubleButton.onClick.AddListener(DoubleBet);
        hitButton.interactable = false;
        standButton.interactable = false;
        doubleButton.interactable = false;
        UpdateBalanceUI();
    }

    void OnEnable() => UpdateBalanceUI();

    void UpdateBalanceUI()
    {
        double balance = BalanceManager.GetBalance();
        if (balanceText != null) balanceText.text = "Ваш баланс: " + balance.ToString("F0");
    }

    void HideStartButtonAndInput()
    {
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (betInput != null) betInput.gameObject.SetActive(false);
    }

    void ShowStartButtonAndInput()
    {
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (betInput != null) betInput.gameObject.SetActive(true);
    }

    void StartGame()
    {
        HideStartButtonAndInput();

        if (!double.TryParse(betInput.text, out betAmount) || betAmount <= 0)
        {
            gameStatusText.text = "Введите сумму ставки!";
            ShowStartButtonAndInput();
            return;
        }
        if (BalanceManager.GetBalance() < betAmount)
        {
            gameStatusText.text = "Недостаточно денег!";
            ShowStartButtonAndInput();
            return;
        }

        BalanceManager.SpendMoney(betAmount);
        originalBet = betAmount;
        hasDoubled = false;
        isNewGame = true;
        UpdateBalanceUI();

        ClearTable();
        CreateDeck();
        ShuffleDeck();
        gameActive = true;
        playerTurn = true;
        playerHand.Clear();
        dealerHand.Clear();
        DealCards();

        hitButton.interactable = true;
        standButton.interactable = true;
        doubleButton.interactable = true;
        startButton.interactable = false;
        gameStatusText.text = "Ваш ход. Берите карту или останавливайтесь";
        UpdateScores();
    }

    void DoubleBet()
    {
        if (!gameActive || !playerTurn || hasDoubled) return;
        if (BalanceManager.GetBalance() < betAmount)
        {
            gameStatusText.text = "Недостаточно денег для удвоения!";
            return;
        }
        BalanceManager.SpendMoney(betAmount);
        betAmount *= 2;
        hasDoubled = true;
        UpdateBalanceUI();
        gameStatusText.text = "Ставка удвоена! Вы получаете одну карту";
        playerHand.Add(deck[0]); deck.RemoveAt(0);
        UpdateHandsDisplay();
        playerScore = CalculateHandValue(playerHand);
        UpdateScores();
        if (playerScore > 21) EndGame(false);
        else Stand();
        doubleButton.interactable = false;
    }

    void CreateDeck()
    {
        deck.Clear();
        Player_card.Cardtype[] types = {
            Player_card.Cardtype.Two, Player_card.Cardtype.Three, Player_card.Cardtype.Four,
            Player_card.Cardtype.Five, Player_card.Cardtype.Six, Player_card.Cardtype.Seven,
            Player_card.Cardtype.Eight, Player_card.Cardtype.Nine, Player_card.Cardtype.Ten,
            Player_card.Cardtype.Jack, Player_card.Cardtype.Queen, Player_card.Cardtype.King,
            Player_card.Cardtype.Ace
        };
        foreach (Player_card.Cardshit suit in System.Enum.GetValues(typeof(Player_card.Cardshit)))
            foreach (Player_card.Cardtype type in types)
            {
                GameObject go = new GameObject("Card");
                Player_card card = go.AddComponent<Player_card>();
                card.SetCard(suit, type);
                deck.Add(card);
                go.SetActive(false);
            }
    }

    void ShuffleDeck() { for (int i = deck.Count - 1; i > 0; i--) { int r = Random.Range(0, i + 1); (deck[i], deck[r]) = (deck[r], deck[i]); } }

    void DealCards()
    {
        playerHand.Add(deck[0]); deck.RemoveAt(0);
        playerHand.Add(deck[0]); deck.RemoveAt(0);
        dealerHand.Add(deck[0]); deck.RemoveAt(0);
        dealerHand.Add(deck[0]); deck.RemoveAt(0);
        UpdateHandsDisplay();
        playerScore = CalculateHandValue(playerHand);
        dealerScore = CalculateHandValue(dealerHand);
        UpdateScores();
        if (playerScore == 21) EndGame(true);
    }

    int CalculateHandValue(List<Player_card> hand)
    {
        int value = 0, aceCount = 0;
        foreach (var card in hand)
        {
            int v = GetCardValue(card.GetCardtype());
            if (v == 11) aceCount++;
            value += v;
        }
        while (value > 21 && aceCount > 0) { value -= 10; aceCount--; }
        return value;
    }

    int GetCardValue(Player_card.Cardtype type) => type switch
    {
        Player_card.Cardtype.Two => 2,
        Player_card.Cardtype.Three => 3,
        Player_card.Cardtype.Four => 4,
        Player_card.Cardtype.Five => 5,
        Player_card.Cardtype.Six => 6,
        Player_card.Cardtype.Seven => 7,
        Player_card.Cardtype.Eight => 8,
        Player_card.Cardtype.Nine => 9,
        Player_card.Cardtype.Ten => 10,
        Player_card.Cardtype.Jack => 10,
        Player_card.Cardtype.Queen => 10,
        Player_card.Cardtype.King => 10,
        Player_card.Cardtype.Ace => 11,
        _ => 0
    };

    void Hit()
    {
        if (!gameActive || !playerTurn) return;
        playerHand.Add(deck[0]); deck.RemoveAt(0);
        UpdateHandsDisplay();
        playerScore = CalculateHandValue(playerHand);
        UpdateScores();
        if (playerScore > 21) EndGame(false);
        else if (playerScore == 21) Stand();
    }

    void Stand()
    {
        if (!gameActive) return;
        playerTurn = false;
        hitButton.interactable = false;
        standButton.interactable = false;
        doubleButton.interactable = false;
        gameStatusText.text = "Ход дилера...";
        UpdateHandsDisplay();
        StartCoroutine(DealerTurn());
    }

    IEnumerator DealerTurn()
    {
        yield return new WaitForSeconds(1f);
        RevealDealerCards();
        dealerScore = CalculateHandValue(dealerHand);
        UpdateScores();
        yield return new WaitForSeconds(1f);
        while (dealerScore < 17)
        {
            dealerHand.Add(deck[0]); deck.RemoveAt(0);
            UpdateHandsDisplay();
            dealerScore = CalculateHandValue(dealerHand);
            UpdateScores();
            yield return new WaitForSeconds(0.8f);
        }
        DetermineWinner();
    }

    void RevealDealerCards()
    {
        foreach (Transform child in dealerHandParent) Destroy(child.gameObject);
        float offset = 0;
        foreach (var card in dealerHand)
        {
            GameObject go = Instantiate(cardPrefab, dealerHandParent);
            go.GetComponent<CardVisual>().SetCard(card, GetCardSprite(card));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(138, 206);
            rect.anchoredPosition = new Vector2(offset, 0);
            offset += 90f;
        }
    }

    void DetermineWinner()
    {
        if (dealerScore > 21) EndGame(true);
        else if (playerScore > dealerScore) EndGame(true);
        else if (dealerScore > playerScore) EndGame(false);
        else EndGame(null);
    }

    void EndGame(bool? playerWon)
    {
        gameActive = false;
        isNewGame = false;
        hitButton.interactable = false;
        standButton.interactable = false;
        doubleButton.interactable = false;
        startButton.interactable = true;

        if (playerWon == true)
        {
            double win = betAmount * 2;
            BalanceManager.AddMoney(win);
            gameStatusText.text = "ПОБЕДА! +" + win.ToString("F0") + " монет!";
        }
        else if (playerWon == false) gameStatusText.text = "ПОРАЖЕНИЕ! -" + originalBet.ToString("F0") + " монет!";
        else
        {
            BalanceManager.AddMoney(originalBet);
            gameStatusText.text = "НИЧЬЯ! Ставка возвращена.";
        }
        UpdateBalanceUI();
        ShowStartButtonAndInput();
        BalanceManager.OnBalanceChanged?.Invoke();
    }

    void UpdateHandsDisplay()
    {
        foreach (Transform child in playerHandParent) Destroy(child.gameObject);
        foreach (Transform child in dealerHandParent) Destroy(child.gameObject);

        float offset = 0;
        foreach (var card in playerHand)
        {
            GameObject go = Instantiate(cardPrefab, playerHandParent);
            go.GetComponent<CardVisual>().SetCard(card, GetCardSprite(card));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(138, 206);
            rect.anchoredPosition = new Vector2(offset, 0);
            offset += 90f;
        }

        offset = 0;
        for (int i = 0; i < dealerHand.Count; i++)
        {
            GameObject go = Instantiate(cardPrefab, dealerHandParent);
            var visual = go.GetComponent<CardVisual>();
            if (i == 0) visual.SetCard(dealerHand[i], GetCardSprite(dealerHand[i]));
            else if (isNewGame && gameActive && playerTurn) visual.SetFaceDown(cardBackSprite);
            else visual.SetCard(dealerHand[i], GetCardSprite(dealerHand[i]));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(138, 206);
            rect.anchoredPosition = new Vector2(offset, 0);
            offset += 90f;
        }
    }

    void UpdateScores()
    {
        playerScoreText.text = "Ваши очки: " + playerScore;
        if (playerTurn && gameActive && isNewGame) dealerScoreText.text = "Очки дилера: ?";
        else dealerScoreText.text = "Очки дилера: " + dealerScore;
    }

    Sprite GetCardSprite(Player_card card)
    {
        string rank = GetRankString(card.GetCardtype());
        string suit = GetSuitString(card.GetSuit());
        return Resources.Load<Sprite>($"Cards/{rank}_of_{suit}");
    }

    string GetRankString(Player_card.Cardtype type) => type switch
    {
        Player_card.Cardtype.Two => "2",
        Player_card.Cardtype.Three => "3",
        Player_card.Cardtype.Four => "4",
        Player_card.Cardtype.Five => "5",
        Player_card.Cardtype.Six => "6",
        Player_card.Cardtype.Seven => "7",
        Player_card.Cardtype.Eight => "8",
        Player_card.Cardtype.Nine => "9",
        Player_card.Cardtype.Ten => "10",
        Player_card.Cardtype.Jack => "jack",
        Player_card.Cardtype.Queen => "queen",
        Player_card.Cardtype.King => "king",
        Player_card.Cardtype.Ace => "ace",
        _ => ""
    };

    string GetSuitString(Player_card.Cardshit suit) => suit switch
    {
        Player_card.Cardshit.Hearts => "hearts",
        Player_card.Cardshit.Diamonds => "diamonds",
        Player_card.Cardshit.Clubs => "clubs",
        Player_card.Cardshit.Spades => "spades",
        _ => ""
    };

    void ClearTable()
    {
        foreach (var v in allCardVisuals) if (v != null) Destroy(v.gameObject);
        allCardVisuals.Clear();
        playerHand.Clear();
        dealerHand.Clear();
        foreach (Transform child in playerHandParent) Destroy(child.gameObject);
        foreach (Transform child in dealerHandParent) Destroy(child.gameObject);
    }

    public void BackToMenu()
    {
        BalanceManager.OnBalanceChanged?.Invoke();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}