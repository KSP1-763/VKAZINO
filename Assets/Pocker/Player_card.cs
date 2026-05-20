using UnityEngine;

public class Player_card : MonoBehaviour
{
    public enum Cardshit { Clubs, Diamonds, Hearts, Spades }
    public enum Cardtype { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    [SerializeField] private Cardshit cardshit;
    [SerializeField] private Cardtype cardtype;

    public void SetCard(Cardshit suit, Cardtype type)
    {
        cardshit = suit;
        cardtype = type;
    }

    public Cardshit GetSuit() => cardshit;
    public Cardtype GetCardtype() => cardtype;

    public string GetCardName()
    {
        return cardshit + " " + cardtype;
    }
}