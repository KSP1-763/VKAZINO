using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager_Poker : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField betInput;
    public Button startButton;
    public Button raiseButton, callButton, foldButton;
    public TextMeshProUGUI playerBalanceText;
    public TextMeshProUGUI potText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI comboText;

    [Header("Родители карт")]
    public Transform playerHandParent;
    public Transform robotHandParent;
    public Transform communityParent;

    [Header("Префабы")]
    public GameObject cardPrefab;
    public Sprite cardBackSprite;

    private Deck deck;
    private double playerBalance;
    private double currentBet;
    private double pot;

    private List<Player_card> playerHand = new List<Player_card>();
    private List<Player_card> robotHand = new List<Player_card>();
    private List<Player_card> communityCards = new List<Player_card>();
    private List<CardVisual> allCardVisuals = new List<CardVisual>();

    private bool gameActive = false;
    private bool playerTurn = true;
    private double playerBet = 0;
    private double robotBet = 0;
    private string stage = "preflop";

    void Start()
    {
        deck = GetComponent<Deck>();
        startButton.onClick.AddListener(StartGame);
        raiseButton.onClick.AddListener(Raise);
        callButton.onClick.AddListener(Call);
        foldButton.onClick.AddListener(Fold);

        raiseButton.interactable = false;
        callButton.interactable = false;
        foldButton.interactable = false;

        LoadBalance();
        UpdateBalanceUI();
        pot = 0;
        UpdatePotUI();
    }

    void OnEnable()
    {
        LoadBalance();
        UpdateBalanceUI();
    }

    void LoadBalance() => playerBalance = BalanceManager.GetBalance();
    void SaveBalance() => BalanceManager.SetBalance(playerBalance);
    void UpdateBalanceUI() => playerBalanceText.text = "Ваш баланс: " + playerBalance.ToString("F0");
    void UpdatePotUI() => potText.text = "Банк: " + pot.ToString("F0");

    void StartGame()
    {
        if (!double.TryParse(betInput.text, out currentBet) || currentBet <= 0)
        {
            resultText.text = "Введите сумму ставки!";
            return;
        }
        if (playerBalance < currentBet)
        {
            resultText.text = "Недостаточно средств!";
            return;
        }

        pot = currentBet * 2;
        playerBalance -= currentBet;
        playerBet = currentBet;
        robotBet = currentBet;
        SaveBalance();
        UpdateBalanceUI();
        UpdatePotUI();

        ClearTable();
        deck.Shuffle();
        playerHand.Clear();
        robotHand.Clear();
        communityCards.Clear();
        allCardVisuals.Clear();

        Player_card[] allCards = deck.GetAllCards();
        int idx = 0;
        for (int i = 0; i < 2; i++)
        {
            playerHand.Add(allCards[idx++]);
            robotHand.Add(allCards[idx++]);
        }

        ShowCards(playerHandParent, playerHand, true);
        ShowCards(robotHandParent, robotHand, false);

        gameActive = true;
        playerTurn = true;
        stage = "preflop";
        //resultText.text = "Префлоп. Ваш ход. Повысить/Уравнять/Пас";

        raiseButton.interactable = true;
        callButton.interactable = true;
        foldButton.interactable = true;
        startButton.interactable = false;
    }

    void Raise()
    {
        if (!gameActive || !playerTurn) return;
        double raiseAmount = currentBet;
        if (playerBalance < raiseAmount)
        {
            resultText.text = "Недостаточно денег!";
            return;
        }

        playerBalance -= raiseAmount;
        playerBet += raiseAmount;
        pot += raiseAmount;
        SaveBalance();
        UpdateBalanceUI();
        UpdatePotUI();

        resultText.text = $"Вы повысили на {raiseAmount:F0}";
        playerTurn = false;
        StartCoroutine(RobotAction());
    }

    void Call()
    {
        if (!gameActive || !playerTurn) return;
        double need = robotBet - playerBet;
        if (need > 0)
        {
            if (playerBalance < need)
            {
                resultText.text = "Не хватает денег для уравнивания!";
                return;
            }
            playerBalance -= need;
            playerBet += need;
            pot += need;
            SaveBalance();
            UpdateBalanceUI();
            UpdatePotUI();
            resultText.text = $"Вы уравняли ставку ({need:F0})";
        }
        else
        {
            resultText.text = "Вы пропустили ход (чек)";
        }

        playerTurn = false;
        StartCoroutine(RobotAction());
    }

    void Fold()
    {
        if (!gameActive || !playerTurn) return;
        ShowRobotCards(); // показываем карты робота
        resultText.text = "Вы сбросили карты. Поражение!";
        gameActive = false;
        raiseButton.interactable = false;
        callButton.interactable = false;
        foldButton.interactable = false;
        startButton.interactable = true;
    }

    IEnumerator RobotAction()
    {
        yield return new WaitForSeconds(1f);
        if (!gameActive) yield break;

        double need = playerBet - robotBet;
        double robotBalance = 10000;

        if (need > 0)
        {
            if (robotBalance >= need)
            {
                robotBet += need;
                pot += need;
                resultText.text = $"Робот уравнял ставку ({need:F0})";
            }
            else
            {
                ShowRobotCards();
                resultText.text = "Робот сбросил карты. Вы выиграли!";
                playerBalance += pot;
                SaveBalance();
                UpdateBalanceUI();
                EndGame(true);
                yield break;
            }
        }
        else
        {
            resultText.text = "Робот чекает";
        }

        NextStage();
    }

    void NextStage()
    {
        playerTurn = true;
        playerBet = 0;
        robotBet = 0;

        switch (stage)
        {
            case "preflop":
                stage = "flop";
                //resultText.text = "Флоп. Ваш ход.";
                AddCommunityCard();
                AddCommunityCard();
                AddCommunityCard();
                break;
            case "flop":
                stage = "turn";
                //resultText.text = "Тёрн. Ваш ход.";
                AddCommunityCard();
                break;
            case "turn":
                stage = "river";
                //resultText.text = "Ривер. Ваш ход.";
                AddCommunityCard();
                break;
            case "river":
                ShowRobotCards();
                DetermineWinner();
                return;
        }
        UpdateHandsDisplay();
        raiseButton.interactable = true;
        callButton.interactable = true;
        foldButton.interactable = true;
    }

    void ShowRobotCards()
    {
        ShowCards(robotHandParent, robotHand, true);
    }

    void AddCommunityCard()
    {
        Player_card[] allCards = deck.GetAllCards();
        for (int i = 0; i < allCards.Length; i++)
        {
            if (!playerHand.Contains(allCards[i]) && !robotHand.Contains(allCards[i]) && !communityCards.Contains(allCards[i]))
            {
                communityCards.Add(allCards[i]);
                break;
            }
        }
        ShowCards(communityParent, communityCards, true);
    }

    void DetermineWinner()
    {
        List<Player_card> playerFull = new List<Player_card>(playerHand);
        playerFull.AddRange(communityCards);
        List<Player_card> robotFull = new List<Player_card>(robotHand);
        robotFull.AddRange(communityCards);

        HandResult playerResult = EvaluateHand(playerFull);
        HandResult robotResult = EvaluateHand(robotFull);
        comboText.text = "Вы: " + GetHandName(playerResult.rank) + "\nРобот: " + GetHandName(robotResult.rank);

        int comparison = CompareHands(playerResult, robotResult);
        if (comparison > 0)
        {
            playerBalance += pot;
            SaveBalance();
            resultText.text = "ПОБЕДА! +" + pot.ToString("F0") + " монет!";
        }
        else if (comparison < 0)
        {
            resultText.text = "ПОРАЖЕНИЕ! -" + currentBet.ToString("F0") + " монет!";
        }
        else
        {
            double half = pot / 2;
            playerBalance += half;
            SaveBalance();
            resultText.text = "НИЧЬЯ! Возвращено " + half.ToString("F0") + " монет";
        }
        UpdateBalanceUI();
        EndGame(comparison > 0);
    }

    void EndGame(bool win)
    {
        gameActive = false;
        raiseButton.interactable = false;
        callButton.interactable = false;
        foldButton.interactable = false;
        startButton.interactable = true;
        if (!win) resultText.text = "Вы проиграли раунд";
    }

    void ShowCards(Transform parent, List<Player_card> cards, bool faceUp)
    {
        foreach (Transform child in parent) Destroy(child.gameObject);
        foreach (var card in cards)
        {
            GameObject cardGO = Instantiate(cardPrefab, parent);
            CardVisual visual = cardGO.GetComponent<CardVisual>();
            allCardVisuals.Add(visual);
            if (faceUp) visual.SetCard(card, GetCardSprite(card));
            else visual.SetFaceDown(cardBackSprite);
        }
    }

    void UpdateHandsDisplay()
    {
        ShowCards(playerHandParent, playerHand, true);
        ShowCards(robotHandParent, robotHand, false);
        ShowCards(communityParent, communityCards, true);
    }

    Sprite GetCardSprite(Player_card card)
    {
        string rank = card.GetCardtype().ToString().ToLower();
        string suit = card.GetSuit().ToString().ToLower();
        rank = rank switch
        {
            "two" => "2",
            "three" => "3",
            "four" => "4",
            "five" => "5",
            "six" => "6",
            "seven" => "7",
            "eight" => "8",
            "nine" => "9",
            "ten" => "10",
            "jack" => "jack",
            "queen" => "queen",
            "king" => "king",
            "ace" => "ace",
            _ => rank
        };
        suit = suit switch
        {
            "clubs" => "clubs",
            "diamonds" => "diamonds",
            "hearts" => "hearts",
            "spades" => "spades",
            _ => suit
        };
        return Resources.Load<Sprite>($"Cards/{rank}_of_{suit}");
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
        Player_card.Cardtype.Jack => 11,
        Player_card.Cardtype.Queen => 12,
        Player_card.Cardtype.King => 13,
        Player_card.Cardtype.Ace => 14,
        _ => 0
    };

    enum HandRank
    {
        HighCard, OnePair, TwoPair, ThreeOfKind, Straight,
        Flush, FullHouse, FourOfKind, StraightFlush, RoyalFlush
    }

    class HandResult { public HandRank rank; public List<int> tieBreakers = new List<int>(); }

    HandResult EvaluateHand(List<Player_card> cards)
    {
        var values = cards.Select(c => GetCardValue(c.GetCardtype())).ToList();
        var suits = cards.Select(c => c.GetSuit()).ToList();
        values.Sort((a, b) => b.CompareTo(a));

        bool isFlush = suits.GroupBy(s => s).Any(g => g.Count() >= 5);
        var distinct = values.Distinct().OrderBy(v => v).ToList();
        bool isStraight = false;
        int straightHigh = 0;
        for (int i = 0; i <= distinct.Count - 5; i++)
            if (distinct[i + 4] - distinct[i] == 4) { isStraight = true; straightHigh = distinct[i + 4]; break; }
        if (!isStraight && distinct.Contains(14) && distinct.Contains(2) &&
            distinct.Contains(3) && distinct.Contains(4) && distinct.Contains(5))
        { isStraight = true; straightHigh = 5; }

        var groups = values.GroupBy(v => v).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        if (groups.Count == 0) return new HandResult { rank = HandRank.HighCard, tieBreakers = { 0 } };

        int gc = groups[0].Count();
        int sc = groups.Count > 1 ? groups[1].Count() : 0;
        HandResult res = new HandResult();

        if (isFlush && isStraight && straightHigh == 14) res.rank = HandRank.RoyalFlush;
        else if (isFlush && isStraight) res.rank = HandRank.StraightFlush;
        else if (gc == 4) res.rank = HandRank.FourOfKind;
        else if (gc == 3 && sc >= 2) res.rank = HandRank.FullHouse;
        else if (isFlush) res.rank = HandRank.Flush;
        else if (isStraight) res.rank = HandRank.Straight;
        else if (gc == 3) res.rank = HandRank.ThreeOfKind;
        else if (gc == 2 && sc == 2) res.rank = HandRank.TwoPair;
        else if (gc == 2) res.rank = HandRank.OnePair;
        else res.rank = HandRank.HighCard;

        foreach (var g in groups)
            for (int i = 0; i < g.Count(); i++) res.tieBreakers.Add(g.Key);
        if (isStraight) { res.tieBreakers.Clear(); res.tieBreakers.Add(straightHigh); }
        return res;
    }

    int CompareHands(HandResult p, HandResult r)
    {
        if (p.rank != r.rank) return p.rank > r.rank ? 1 : -1;
        for (int i = 0; i < p.tieBreakers.Count && i < r.tieBreakers.Count; i++)
            if (p.tieBreakers[i] != r.tieBreakers[i]) return p.tieBreakers[i] > r.tieBreakers[i] ? 1 : -1;
        return 0;
    }

    string GetHandName(HandRank rank) => rank switch
    {
        HandRank.RoyalFlush => "Флеш-рояль",
        HandRank.StraightFlush => "Стрит-флеш",
        HandRank.FourOfKind => "Каре",
        HandRank.FullHouse => "Фулл-хаус",
        HandRank.Flush => "Флеш",
        HandRank.Straight => "Стрит",
        HandRank.ThreeOfKind => "Сет",
        HandRank.TwoPair => "Две пары",
        HandRank.OnePair => "Пара",
        _ => "Старшая карта"
    };

    void ClearTable()
    {
        foreach (var visual in allCardVisuals) if (visual != null) Destroy(visual.gameObject);
        allCardVisuals.Clear();
        foreach (Transform child in playerHandParent) Destroy(child.gameObject);
        foreach (Transform child in robotHandParent) Destroy(child.gameObject);
        foreach (Transform child in communityParent) Destroy(child.gameObject);
    }

    public void BackToMenu() => SceneManager.LoadScene("MainMenu");
}