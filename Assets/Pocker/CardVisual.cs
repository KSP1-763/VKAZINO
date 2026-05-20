using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardVisual : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image cardImage;
    private Player_card cardData;
    private System.Action<Player_card> onClickCallback;

    public void SetCard(Player_card card, Sprite cardSprite)
    {
        cardData = card;
        cardImage.sprite = cardSprite;
    }

    public void SetFaceDown(Sprite backSprite)
    {
        cardImage.sprite = backSprite;
    }

    public void SetOnClickCallback(System.Action<Player_card> callback)
    {
        onClickCallback = callback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onClickCallback != null && cardData != null)
        {
            onClickCallback(cardData);
        }
    }
}