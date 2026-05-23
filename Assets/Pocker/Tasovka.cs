using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tasovka : MonoBehaviour
{
    private List<Player_card> cards = new List<Player_card>();

    void Start()
    {
        CreateDeck();
        New_tasovka();
    }

    void CreateDeck()
    {
        cards.Clear();
        // Создаём все 52 карты в коде
        foreach (Player_card.Cardshit suit in System.Enum.GetValues(typeof(Player_card.Cardshit)))
        {
            foreach (Player_card.Cardtype type in System.Enum.GetValues(typeof(Player_card.Cardtype)))
            {
                GameObject cardGO = new GameObject();
                Player_card card = cardGO.AddComponent<Player_card>();
                // Тут нужно установить масть и тип через public поля или методы
                // Так как ваши поля private, нужно их сделать public или добавить метод
                cards.Add(card);
                Destroy(cardGO);
            }
        }
    }

    public void New_tasovka()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int random = Random.Range(0, i + 1);
            (cards[i], cards[random]) = (cards[random], cards[i]);
        }
    }

    public Player_card GetCard(int index)
    {
        if (index >= 0 && index < cards.Count)
            return cards[index];
        return null;
    }

    public Player_card[] GetAllCards()
    {
        return cards.ToArray();
    }
}