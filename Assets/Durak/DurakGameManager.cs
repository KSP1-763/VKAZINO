using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DurakGameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI trumpText;
    public TextMeshProUGUI gameStatusText;
    public TextMeshProUGUI playerCardsCountText;
    public TextMeshProUGUI aiCardsCountText;
    public TextMeshProUGUI deckCountText;
    public Button startButton;
    public Button passButton;
    public Button takeButton;

    [Header("Родители карт")]
    public Transform playerHandParent;
    public Transform aiHandParent;
    public Transform tableParent;

    [Header("Префабы")]
    public GameObject cardPrefab;
    public Sprite cardBackSprite;

    private List<Player_card> deck = new List<Player_card>();
    private List<Player_card> playerHand = new List<Player_card>();
    private List<Player_card> aiHand = new List<Player_card>();
    private List<Player_card> tableCards = new List<Player_card>();

    private Player_card trumpCard;
    private bool isPlayerTurn;
    private bool isDefensePhase;
    private Player_card currentAttackCard;
    private bool gameActive = false;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        passButton.onClick.AddListener(PassTurn);
        takeButton.onClick.AddListener(TakeCards);

        passButton.interactable = false;
        takeButton.interactable = false;
    }

    void StartGame()
    {
        ClearAll();
        CreateDeck();
        ShuffleDeck();
        SetTrump();
        DealCards();

        gameActive = true;
        isDefensePhase = false;
        isPlayerTurn = DetermineFirstPlayer();

        UpdateUI();
        UpdateHandsDisplay();

        if (isPlayerTurn)
        {
            gameStatusText.text = "Ваш ход. Выберите карту для атаки";
        }
        else
        {
            gameStatusText.text = "Ход компьютера...";
            StartCoroutine(AIPlay());
        }

        passButton.interactable = false;
        takeButton.interactable = false;
    }

    void CreateDeck()
    {
        deck.Clear();

        Player_card.Cardtype[] types = {
            Player_card.Cardtype.Six, Player_card.Cardtype.Seven, Player_card.Cardtype.Eight,
            Player_card.Cardtype.Nine, Player_card.Cardtype.Ten, Player_card.Cardtype.Jack,
            Player_card.Cardtype.Queen, Player_card.Cardtype.King, Player_card.Cardtype.Ace
        };

        foreach (Player_card.Cardshit suit in System.Enum.GetValues(typeof(Player_card.Cardshit)))
        {
            foreach (Player_card.Cardtype type in types)
            {
                GameObject go = new GameObject("Card");
                Player_card card = go.AddComponent<Player_card>();
                card.SetCard(suit, type);
                deck.Add(card);
                go.SetActive(false);
            }
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
        trumpText.text = "Козырь: " + GetCardName(trumpCard);
    }

    string GetCardName(Player_card card)
    {
        string suit = "";
        switch (card.GetSuit())
        {
            case Player_card.Cardshit.Hearts: suit = "♥"; break;
            case Player_card.Cardshit.Diamonds: suit = "♦"; break;
            case Player_card.Cardshit.Clubs: suit = "♣"; break;
            case Player_card.Cardshit.Spades: suit = "♠"; break;
        }
        return suit;
    }

    void DealCards()
    {
        for (int i = 0; i < 6; i++)
        {
            if (deck.Count > 0)
            {
                playerHand.Add(deck[0]);
                deck.RemoveAt(0);
            }
            if (deck.Count > 0)
            {
                aiHand.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }
    }

    bool DetermineFirstPlayer()
    {
        int playerTrumpValue = GetLowestTrumpValue(playerHand);
        int aiTrumpValue = GetLowestTrumpValue(aiHand);

        if (playerTrumpValue <= aiTrumpValue)
            return true;
        else
            return false;
    }

    int GetLowestTrumpValue(List<Player_card> hand)
    {
        int lowest = 100;
        foreach (var card in hand)
        {
            if (card.GetSuit() == trumpCard.GetSuit())
            {
                int val = (int)card.GetCardtype();
                if (val < lowest) lowest = val;
            }
        }
        return lowest;
    }

    public void OnCardClicked(Player_card card)
    {
        if (!gameActive) return;

        if (!isDefensePhase && isPlayerTurn)
        {
            Attack(card);
        }
        else if (isDefensePhase && isPlayerTurn)
        {
            Defend(card);
        }
    }

    void Attack(Player_card card)
    {
        if (!playerHand.Contains(card)) return;

        if (tableCards.Count > 0)
        {
            bool canAttack = false;
            foreach (var tableCard in tableCards)
            {
                if (tableCard.GetCardtype() == card.GetCardtype())
                {
                    canAttack = true;
                    break;
                }
            }
            if (!canAttack)
            {
                gameStatusText.text = "Можно подкидывать только карты того же достоинства";
                return;
            }
        }

        playerHand.Remove(card);
        tableCards.Add(card);
        currentAttackCard = card;

        UpdateHandsDisplay();
        ShowCardOnTable(card, true);

        isDefensePhase = true;
        isPlayerTurn = false;
        gameStatusText.text = "Компьютер защищается...";
        passButton.interactable = true;
        takeButton.interactable = true;
        StartCoroutine(AIPlay());
    }

    void Defend(Player_card card)
    {
        if (!playerHand.Contains(card)) return;

        if (CanDefend(currentAttackCard, card))
        {
            playerHand.Remove(card);
            tableCards.Add(card);
            ShowCardOnTable(card, false);
            UpdateHandsDisplay();

            bool attackerCanAdd = CanAttackerAddMoreCards();

            if (attackerCanAdd)
            {
                isDefensePhase = false;
                isPlayerTurn = true;
                gameStatusText.text = "Можете подкинуть карту того же достоинства";
            }
            else
            {
                StartCoroutine(EndRound(true));
            }
        }
        else
        {
            gameStatusText.text = "Эту карту нельзя отбить";
        }
    }

    bool CanDefend(Player_card attack, Player_card defense)
    {
        int attackValue = (int)attack.GetCardtype();
        int defenseValue = (int)defense.GetCardtype();

        bool attackIsTrump = attack.GetSuit() == trumpCard.GetSuit();
        bool defenseIsTrump = defense.GetSuit() == trumpCard.GetSuit();

        if (defenseIsTrump && !attackIsTrump)
            return true;

        if (defenseIsTrump && attackIsTrump && defenseValue > attackValue)
            return true;

        if (defense.GetSuit() == attack.GetSuit() && defenseValue > attackValue)
            return true;

        return false;
    }

    bool CanAttackerAddMoreCards()
    {
        List<Player_card> attackerHand = isPlayerTurn ? aiHand : playerHand;

        foreach (var card in attackerHand)
        {
            foreach (var tableCard in tableCards)
            {
                if (card.GetCardtype() == tableCard.GetCardtype())
                    return true;
            }
        }
        return false;
    }

    IEnumerator EndRound(bool cardsToDiscard)
    {
        yield return new WaitForSeconds(1f);

        if (cardsToDiscard)
        {
            gameStatusText.text = "Карты ушли в бито";
        }

        tableCards.Clear();
        ClearTableCards();

        DrawToSix();

        if (playerHand.Count == 0)
        {
            gameStatusText.text = "ПОБЕДА! Вы выиграли!";
            gameActive = false;
            passButton.interactable = false;
            takeButton.interactable = false;
            yield break;
        }

        if (aiHand.Count == 0)
        {
            gameStatusText.text = "ПОРАЖЕНИЕ! Вы дурак!";
            gameActive = false;
            passButton.interactable = false;
            takeButton.interactable = false;
            yield break;
        }

        bool defenderWasPlayer = isPlayerTurn;

        isDefensePhase = false;
        passButton.interactable = false;
        takeButton.interactable = false;

        UpdateHandsDisplay();

        if (defenderWasPlayer)
        {
            isPlayerTurn = true;
            gameStatusText.text = "Ваш ход. Выберите карту для атаки";
        }
        else
        {
            isPlayerTurn = false;
            gameStatusText.text = "Ход компьютера...";
            StartCoroutine(AIPlay());
        }
    }

    void DrawToSix()
    {
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

        if (deckCountText != null)
            deckCountText.text = "Колода: " + deck.Count;
    }

    void PassTurn()
    {
        if (!gameActive) return;
        if (!isDefensePhase) return;

        TakeCards();
    }

    void TakeCards()
    {
        if (!gameActive) return;
        if (!isDefensePhase) return;

        foreach (var card in tableCards)
        {
            if (isPlayerTurn)
                playerHand.Add(card);
            else
                aiHand.Add(card);
        }
        tableCards.Clear();
        ClearTableCards();

        DrawToSix();
        UpdateHandsDisplay();

        bool attackerWasPlayer = !isPlayerTurn;

        isDefensePhase = false;
        passButton.interactable = false;
        takeButton.interactable = false;

        if (playerHand.Count == 0)
        {
            gameStatusText.text = "ПОБЕДА! Вы выиграли!";
            gameActive = false;
            return;
        }

        if (aiHand.Count == 0)
        {
            gameStatusText.text = "ПОРАЖЕНИЕ! Вы дурак!";
            gameActive = false;
            return;
        }

        if (attackerWasPlayer)
        {
            isPlayerTurn = true;
            gameStatusText.text = "Ваш ход. Выберите карту для атаки";
        }
        else
        {
            isPlayerTurn = false;
            gameStatusText.text = "Ход компьютера...";
            StartCoroutine(AIPlay());
        }
    }

    IEnumerator AIPlay()
    {
        yield return new WaitForSeconds(0.8f);

        if (!gameActive) yield break;

        if (!isDefensePhase && !isPlayerTurn)
        {
            AIAttack();
        }
        else if (isDefensePhase && !isPlayerTurn)
        {
            AIDefend();
        }
    }

    void AIAttack()
    {
        if (aiHand.Count == 0) return;

        List<Player_card> possibleCards = new List<Player_card>();

        if (tableCards.Count == 0)
        {
            possibleCards = aiHand;
        }
        else
        {
            foreach (var card in aiHand)
            {
                foreach (var tableCard in tableCards)
                {
                    if (card.GetCardtype() == tableCard.GetCardtype())
                    {
                        possibleCards.Add(card);
                        break;
                    }
                }
            }
        }

        if (possibleCards.Count > 0)
        {
            Player_card cardToAttack = possibleCards[0];
            aiHand.Remove(cardToAttack);
            tableCards.Add(cardToAttack);
            currentAttackCard = cardToAttack;
            ShowCardOnTable(cardToAttack, true);
            UpdateHandsDisplay();

            isDefensePhase = true;
            isPlayerTurn = true;
            gameStatusText.text = "Защититесь! Выберите карту для отбоя";
            passButton.interactable = true;
            takeButton.interactable = true;
        }
        else
        {
            StartCoroutine(EndRound(true));
        }
    }

    void AIDefend()
    {
        Player_card bestDefense = null;

        foreach (var card in aiHand)
        {
            if (CanDefend(currentAttackCard, card))
            {
                if (bestDefense == null || (int)card.GetCardtype() < (int)bestDefense.GetCardtype())
                {
                    bestDefense = card;
                }
            }
        }

        if (bestDefense != null)
        {
            aiHand.Remove(bestDefense);
            tableCards.Add(bestDefense);
            ShowCardOnTable(bestDefense, false);
            UpdateHandsDisplay();

            bool attackerCanAdd = CanAttackerAddMoreCards();

            if (attackerCanAdd)
            {
                isDefensePhase = false;
                isPlayerTurn = false;
                gameStatusText.text = "Компьютер подкидывает карту...";
                StartCoroutine(AIPlay());
            }
            else
            {
                StartCoroutine(EndRound(true));
            }
        }
        else
        {
            foreach (var card in tableCards)
            {
                aiHand.Add(card);
            }
            tableCards.Clear();
            ClearTableCards();
            DrawToSix();
            UpdateHandsDisplay();

            isDefensePhase = false;
            isPlayerTurn = true;
            gameStatusText.text = "Ваш ход. Выберите карту для атаки";
            passButton.interactable = false;
            takeButton.interactable = false;
        }
    }

    void ShowCardOnTable(Player_card card, bool isAttack)
    {
        GameObject cardGO = Instantiate(cardPrefab, tableParent);
        CardVisual visual = cardGO.GetComponent<CardVisual>();
        visual.SetCard(card, GetCardSprite(card));

        float offset = tableCards.Count * 100f;
        RectTransform rect = cardGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(offset, isAttack ? 50 : -50);
    }

    void UpdateHandsDisplay()
    {
        foreach (Transform child in playerHandParent) Destroy(child.gameObject);
        foreach (Transform child in aiHandParent) Destroy(child.gameObject);

        float offset = 0;
        foreach (var card in playerHand)
        {
            GameObject cardGO = Instantiate(cardPrefab, playerHandParent);
            CardVisual visual = cardGO.GetComponent<CardVisual>();
            visual.SetCard(card, GetCardSprite(card));
            visual.SetOnClickCallback(OnCardClicked);
            cardGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, 0);
            offset += 90f;
        }

        offset = 0;
        foreach (var card in aiHand)
        {
            GameObject cardGO = Instantiate(cardPrefab, aiHandParent);
            CardVisual visual = cardGO.GetComponent<CardVisual>();
            visual.SetFaceDown(cardBackSprite);
            cardGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, 0);
            offset += 90f;
        }

        if (playerCardsCountText != null)
            playerCardsCountText.text = "Ваши карты: " + playerHand.Count;
        if (aiCardsCountText != null)
            aiCardsCountText.text = "Карты AI: " + aiHand.Count;
        if (deckCountText != null)
            deckCountText.text = "Колода: " + deck.Count;
    }

    Sprite GetCardSprite(Player_card card)
    {
        string rank = GetRankString(card.GetCardtype());
        string suit = GetSuitString(card.GetSuit());
        Sprite sprite = Resources.Load<Sprite>($"Cards/{rank}_of_{suit}");
        if (sprite == null) Debug.LogError($"Спрайт не найден: Cards/{rank}_of_{suit}");
        return sprite;
    }

    string GetRankString(Player_card.Cardtype type)
    {
        switch (type)
        {
            case Player_card.Cardtype.Six: return "6";
            case Player_card.Cardtype.Seven: return "7";
            case Player_card.Cardtype.Eight: return "8";
            case Player_card.Cardtype.Nine: return "9";
            case Player_card.Cardtype.Ten: return "10";
            case Player_card.Cardtype.Jack: return "jack";
            case Player_card.Cardtype.Queen: return "queen";
            case Player_card.Cardtype.King: return "king";
            case Player_card.Cardtype.Ace: return "ace";
            default: return "";
        }
    }

    string GetSuitString(Player_card.Cardshit suit)
    {
        switch (suit)
        {
            case Player_card.Cardshit.Hearts: return "hearts";
            case Player_card.Cardshit.Diamonds: return "diamonds";
            case Player_card.Cardshit.Clubs: return "clubs";
            case Player_card.Cardshit.Spades: return "spades";
            default: return "";
        }
    }

    void UpdateUI() { }

    void ClearAll()
    {
        foreach (Transform child in playerHandParent) Destroy(child.gameObject);
        foreach (Transform child in aiHandParent) Destroy(child.gameObject);
        foreach (Transform child in tableParent) Destroy(child.gameObject);

        deck.Clear();
        playerHand.Clear();
        aiHand.Clear();
        tableCards.Clear();

        passButton.interactable = false;
        takeButton.interactable = false;
    }

    void ClearTableCards()
    {
        foreach (Transform child in tableParent)
            Destroy(child.gameObject);
    }
}