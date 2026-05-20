using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    private List<Player_card> cards = new List<Player_card>();

    void Awake()
    {
        CreateDeck();
        Shuffle();
    }

    void CreateDeck()
    {
        cards.Clear();

        foreach (Player_card.Cardshit suit in System.Enum.GetValues(typeof(Player_card.Cardshit)))
        {
            foreach (Player_card.Cardtype type in System.Enum.GetValues(typeof(Player_card.Cardtype)))
            {
                GameObject go = new GameObject("Card");
                Player_card card = go.AddComponent<Player_card>();
                card.SetCard(suit, type);
                cards.Add(card);
                go.SetActive(false);
            }
        }
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int random = Random.Range(0, i + 1);
            (cards[i], cards[random]) = (cards[random], cards[i]);
        }
    }

    public Player_card[] GetAllCards()
    {
        return cards.ToArray();
    }
}