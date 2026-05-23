using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DurakGameSimple : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI trumpText, gameStatusText, playerCardsCountText, aiCardsCountText, deckCountText;
    public Button startButton, passButton, takeButton;
    public TMP_InputField betInput;
    public TextMeshProUGUI playerBalanceText;

    [Header("Родители карт")]
    public Transform playerHandParent, aiHandParent, tableParent;

    [Header("Префабы")]
    public GameObject cardPrefab;
    public Sprite cardBackSprite;

    private List<Player_card> deck = new List<Player_card>();
    private List<Player_card> playerHand = new List<Player_card>();
    private List<Player_card> aiHand = new List<Player_card>();
    private List<Player_card> tableCards = new List<Player_card>();

    private Player_card trumpCard;
    private bool gameActive = false;
    private double betAmount = 100;

    private enum GameState { PlayerAttack, AIDefense, AIAttack, PlayerDefense }
    private GameState currentState;
    private Player_card currentAttackCard;

    void Start()
    {
        FindAllButtons();
        startButton?.onClick.AddListener(StartGame);
        passButton?.onClick.AddListener(OnPassClick);
        takeButton?.onClick.AddListener(OnTakeClick);
        if (passButton != null) passButton.interactable = false;
        if (takeButton != null) takeButton.interactable = false;
        UpdateBalanceUI();
    }

    void OnEnable() => UpdateBalanceUI();

    void UpdateBalanceUI()
    {
        double balance = BalanceManager.GetBalance();
        if (playerBalanceText != null)
            playerBalanceText.text = "Ваш баланс: " + balance.ToString("F0");
    }

    void FindAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (Button btn in allButtons)
        {
            if (btn.name == "StartButton" || btn.name == "startButton" || btn.name == "Button_Start" || btn.name == "Start")
                startButton = btn;
            if (btn.name == "PassButton" || btn.name == "Button_pass" || btn.name == "Pass")
                passButton = btn;
            if (btn.name == "TakeButton" || btn.name == "Take_button" || btn.name == "Take")
                takeButton = btn;
        }
    }

    void EnableButtons(bool enable)
    {
        if (passButton != null) passButton.interactable = enable;
        if (takeButton != null) takeButton.interactable = enable;
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

        if (betInput != null && !string.IsNullOrEmpty(betInput.text))
        {
            if (!double.TryParse(betInput.text, out double bet) || bet <= 0)
            {
                gameStatusText.text = "Введите корректную сумму ставки";
                ShowStartButtonAndInput();
                return;
            }
            betAmount = bet;
        }

        double currentBalance = BalanceManager.GetBalance();
        if (currentBalance < betAmount)
        {
            gameStatusText.text = "Недостаточно денег для ставки!";
            ShowStartButtonAndInput();
            return;
        }

        BalanceManager.SpendMoney(betAmount);
        UpdateBalanceUI();

        ClearAll();
        CreateDeck();
        ShuffleDeck();
        SetTrump();
        DealCards();

        gameActive = true;
        bool playerFirst = DetermineFirstPlayer();

        if (playerFirst)
        {
            currentState = GameState.PlayerAttack;
            gameStatusText.text = "Ваша атака. Выберите карту или нажмите Пас";
            if (passButton != null) passButton.interactable = true;
        }
        else
        {
            currentState = GameState.AIAttack;
            gameStatusText.text = "Атака компьютера...";
            StartCoroutine(AITurn());
        }
        UpdateHandsDisplay();
    }

    void CreateDeck()
    {
        deck.Clear();
        Player_card.Cardtype[] types = { Player_card.Cardtype.Six, Player_card.Cardtype.Seven, Player_card.Cardtype.Eight,
            Player_card.Cardtype.Nine, Player_card.Cardtype.Ten, Player_card.Cardtype.Jack,
            Player_card.Cardtype.Queen, Player_card.Cardtype.King, Player_card.Cardtype.Ace };

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

    void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int random = Random.Range(0, i + 1);
            (deck[i], deck[random]) = (deck[random], deck[i]);
        }
    }

    void SetTrump()
    {
        trumpCard = deck[deck.Count - 1];
        string suit = trumpCard.GetSuit() switch
        {
            Player_card.Cardshit.Hearts => "♥",
            Player_card.Cardshit.Diamonds => "♦",
            Player_card.Cardshit.Clubs => "♣",
            Player_card.Cardshit.Spades => "♠",
            _ => ""
        };
        trumpText.text = "Козырь: " + suit;
    }

    void DealCards()
    {
        for (int i = 0; i < 6; i++)
        {
            if (deck.Count > 0) { playerHand.Add(deck[0]); deck.RemoveAt(0); }
            if (deck.Count > 0) { aiHand.Add(deck[0]); deck.RemoveAt(0); }
        }
    }

    bool DetermineFirstPlayer() => GetLowestTrump(playerHand) <= GetLowestTrump(aiHand);
    int GetLowestTrump(List<Player_card> hand) => hand.Where(c => c.GetSuit() == trumpCard.GetSuit()).Select(c => (int)c.GetCardtype()).DefaultIfEmpty(100).Min();

    public void OnCardClicked(Player_card card)
    {
        if (!gameActive) return;
        if (currentState == GameState.PlayerAttack && playerHand.Contains(card)) PlayerAttack(card);
        else if (currentState == GameState.PlayerDefense && playerHand.Contains(card)) PlayerDefense(card);
    }

    void PlayerAttack(Player_card card)
    {
        if (tableCards.Count > 0)
        {
            bool canAdd = tableCards.Any(tc => tc.GetCardtype() == card.GetCardtype());
            if (!canAdd)
            {
                gameStatusText.text = "Можно подкидывать только карты того же достоинства";
                return;
            }
        }
        playerHand.Remove(card);
        tableCards.Add(card);
        currentAttackCard = card;
        ShowCardOnTable(card, true);
        UpdateHandsDisplay();
        currentState = GameState.AIDefense;
        gameStatusText.text = "Компьютер защищается...";
        if (passButton != null) passButton.interactable = false;
        StartCoroutine(AITurn());
    }

    void PlayerDefense(Player_card card)
    {
        if (CanDefend(currentAttackCard, card))
        {
            playerHand.Remove(card);
            tableCards.Add(card);
            ShowCardOnTable(card, false);
            UpdateHandsDisplay();
            if (CanAttackerAddMore())
            {
                currentState = GameState.AIAttack;
                gameStatusText.text = "Компьютер подкидывает карту...";
                StartCoroutine(AITurn());
            }
            else StartCoroutine(EndRound());
        }
        else gameStatusText.text = "Нельзя отбить эту карту";
    }

    IEnumerator AITurn()
    {
        yield return new WaitForSeconds(0.8f);
        if (!gameActive) yield break;
        if (currentState == GameState.AIAttack) AIAttack();
        else if (currentState == GameState.AIDefense) AIDefense();
    }

    void AIAttack()
    {
        List<Player_card> possibleCards = tableCards.Count == 0 ? aiHand : aiHand.Where(c => tableCards.Any(tc => tc.GetCardtype() == c.GetCardtype())).ToList();
        if (possibleCards.Count > 0)
        {
            Player_card card = possibleCards[0];
            aiHand.Remove(card);
            tableCards.Add(card);
            currentAttackCard = card;
            ShowCardOnTable(card, true);
            UpdateHandsDisplay();
            currentState = GameState.PlayerDefense;
            gameStatusText.text = "Защититесь! Выберите карту для отбоя";
            EnableButtons(true);
        }
        else StartCoroutine(EndRound());
    }

    void AIDefense()
    {
        Player_card bestDefense = aiHand.Where(c => CanDefend(currentAttackCard, c)).OrderBy(c => (int)c.GetCardtype()).FirstOrDefault();
        if (bestDefense != null)
        {
            aiHand.Remove(bestDefense);
            tableCards.Add(bestDefense);
            ShowCardOnTable(bestDefense, false);
            UpdateHandsDisplay();
            if (CanAttackerAddMore())
            {
                currentState = GameState.PlayerAttack;
                gameStatusText.text = "Можете подкинуть карту того же достоинства или нажмите Пас";
                if (passButton != null) passButton.interactable = true;
                if (takeButton != null) takeButton.interactable = false;
            }
            else StartCoroutine(EndRound());
        }
        else
        {
            foreach (var card in tableCards) aiHand.Add(card);
            tableCards.Clear();
            ClearTableCards();
            DrawToSix();
            UpdateHandsDisplay();
            currentState = GameState.PlayerAttack;
            gameStatusText.text = "Ваша атака. Выберите карту или нажмите Пас";
            if (passButton != null) passButton.interactable = true;
            if (takeButton != null) takeButton.interactable = false;
        }
    }

    bool CanDefend(Player_card attack, Player_card defense)
    {
        int attackVal = (int)attack.GetCardtype();
        int defenseVal = (int)defense.GetCardtype();
        bool attackTrump = attack.GetSuit() == trumpCard.GetSuit();
        bool defenseTrump = defense.GetSuit() == trumpCard.GetSuit();
        if (defenseTrump && !attackTrump) return true;
        if (defenseTrump && attackTrump && defenseVal > attackVal) return true;
        if (defense.GetSuit() == attack.GetSuit() && defenseVal > attackVal) return true;
        return false;
    }

    bool CanAttackerAddMore()
    {
        List<Player_card> attackerHand = (currentState == GameState.PlayerAttack || currentState == GameState.PlayerDefense) ? aiHand : playerHand;
        return attackerHand.Any(c => tableCards.Any(tc => tc.GetCardtype() == c.GetCardtype()));
    }

    void DrawToSix()
    {
        // Добор карт до 6, если в колоде есть карты
        while (playerHand.Count < 6 && deck.Count > 0)
        {
            playerHand.Add(deck[0]);
            deck.RemoveAt(0);
        }
        while (aiHand.Count < 6 && deck.Count > 0)
        {
            aiHand.Add(deck[0]);
            deck.RemoveAt(0);
        }
        if (deckCountText != null) deckCountText.text = "Колода: " + deck.Count;
    }

    IEnumerator EndRound()
    {
        yield return new WaitForSeconds(1f);
        gameStatusText.text = "Карты ушли в бито";
        tableCards.Clear();
        ClearTableCards();

        // Сначала добираем карты
        DrawToSix();

        // Только после добора проверяем победу
        if (playerHand.Count == 0)
        {
            double winAmount = betAmount * 2;
            BalanceManager.AddMoney(winAmount);
            UpdateBalanceUI();
            gameStatusText.text = "ПОБЕДА! Вы выиграли " + winAmount.ToString("F0") + " монет!";
            gameActive = false;
            EnableButtons(false);
            ShowStartButtonAndInput();
            yield break;
        }
        if (aiHand.Count == 0)
        {
            UpdateBalanceUI();
            gameStatusText.text = "ПОРАЖЕНИЕ! Вы проиграли " + betAmount.ToString("F0") + " монет!";
            gameActive = false;
            EnableButtons(false);
            ShowStartButtonAndInput();
            yield break;
        }

        bool defenderWasPlayer = (currentState == GameState.PlayerDefense);
        if (defenderWasPlayer)
        {
            currentState = GameState.PlayerAttack;
            gameStatusText.text = "Ваша атака. Выберите карту или нажмите Пас";
            if (passButton != null) passButton.interactable = true;
            if (takeButton != null) takeButton.interactable = false;
        }
        else
        {
            currentState = GameState.AIAttack;
            gameStatusText.text = "Атака компьютера...";
            StartCoroutine(AITurn());
        }
        UpdateHandsDisplay();
    }

    void OnPassClick()
    {
        if (!gameActive) return;
        if (currentState == GameState.PlayerDefense) OnTakeClick();
        else if (currentState == GameState.PlayerAttack)
        {
            gameStatusText.text = "Вы пропустили ход";
            tableCards.Clear();
            ClearTableCards();
            currentState = GameState.AIAttack;
            gameStatusText.text = "Атака компьютера...";
            StartCoroutine(AITurn());
            if (passButton != null) passButton.interactable = false;
            if (takeButton != null) takeButton.interactable = false;
        }
    }

    void OnTakeClick()
    {
        if (!gameActive) return;
        if (currentState != GameState.PlayerDefense) return;
        foreach (var card in tableCards) playerHand.Add(card);
        tableCards.Clear();
        ClearTableCards();
        DrawToSix();
        UpdateHandsDisplay();
        currentState = GameState.AIAttack;
        gameStatusText.text = "Атака компьютера...";
        StartCoroutine(AITurn());
        EnableButtons(false);
        UpdateBalanceUI();
    }

    void ShowCardOnTable(Player_card card, bool isAttack)
    {
        GameObject cardGO = Instantiate(cardPrefab, tableParent);
        CardVisual visual = cardGO.GetComponent<CardVisual>();
        visual.SetCard(card, GetCardSprite(card));
        float w = 100f;
        RectTransform rect = cardGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(w, w * 1.4f);
        rect.anchoredPosition = new Vector2(tableCards.Count * (w + 10f), isAttack ? 50 : -50);
    }

    void UpdateHandsDisplay()
    {
        foreach (Transform child in playerHandParent) Destroy(child.gameObject);
        foreach (Transform child in aiHandParent) Destroy(child.gameObject);

        float cardWidth = 120f, cardSpacing = 15f, maxWidth = 1600f;
        float playerScale = playerHand.Count * (cardWidth + cardSpacing) > maxWidth ? maxWidth / (playerHand.Count * (cardWidth + cardSpacing)) : 1f;
        float offset = 0, curW = cardWidth * playerScale, curSp = cardSpacing * playerScale;
        foreach (var card in playerHand)
        {
            GameObject go = Instantiate(cardPrefab, playerHandParent);
            CardVisual visual = go.GetComponent<CardVisual>();
            visual.SetCard(card, GetCardSprite(card));
            visual.SetOnClickCallback(OnCardClicked);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(138,206);
            rect.anchoredPosition = new Vector2(offset, 0);
            offset += curW + curSp;
        }

        float aiScale = aiHand.Count * (cardWidth + cardSpacing) > maxWidth ? maxWidth / (aiHand.Count * (cardWidth + cardSpacing)) : 1f;
        offset = 0; curW = cardWidth * aiScale; curSp = cardSpacing * aiScale;
        foreach (var card in aiHand)
        {
            GameObject go = Instantiate(cardPrefab, aiHandParent);
            CardVisual visual = go.GetComponent<CardVisual>();
            visual.SetFaceDown(cardBackSprite);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(curW, curW * 1.4f);
            rect.anchoredPosition = new Vector2(offset, 0);
            offset += curW + curSp;
        }

        if (playerCardsCountText != null) playerCardsCountText.text = "Ваши карты: " + playerHand.Count;
        if (aiCardsCountText != null) aiCardsCountText.text = "Карты робота: " + aiHand.Count;
        if (deckCountText != null) deckCountText.text = "Колода: " + deck.Count;
    }

    Sprite GetCardSprite(Player_card card)
    {
        string rank = GetRankString(card.GetCardtype());
        string suit = GetSuitString(card.GetSuit());
        Sprite sprite = Resources.Load<Sprite>($"Cards/{rank}_of_{suit}");
        return sprite;
    }

    string GetRankString(Player_card.Cardtype type) => type switch
    {
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

    void ClearAll()
    {
        foreach (Transform child in playerHandParent) Destroy(child.gameObject);
        foreach (Transform child in aiHandParent) Destroy(child.gameObject);
        foreach (Transform child in tableParent) Destroy(child.gameObject);
        deck.Clear();
        playerHand.Clear();
        aiHand.Clear();
        tableCards.Clear();
        EnableButtons(false);
    }

    void ClearTableCards()
    {
        foreach (Transform child in tableParent) Destroy(child.gameObject);
    }

    public void BackToMenu()
    {
        BalanceManager.OnBalanceChanged?.Invoke();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}